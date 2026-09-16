using System.Security.Cryptography;
using System.Text;
using Mall.Api.Domain.Orders;
using Mall.Api.Domain.Payments;
using Mall.Api.Domain.Promotions;
using Mall.Api.Infrastructure;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mall.Api.Domain.Users;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;

namespace Mall.Api.Application.Payments;

public sealed class PaymentUseCases(MallDbContext db, IPaymentGateway gateway, IOptions<PaymentOptions> options)
{
    private readonly PaymentOptions _options = options.Value;

    public async Task<PaymentResult> CreateAsync(long userId, CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == request.OrderId && x.UserId == userId, cancellationToken);
        if (order is null) return new(null, "订单不存在。");
        if (order.Status != OrderStatus.WaitPay) return new(null, "订单当前状态不可支付。");
        var payment = await db.PaymentRecords.SingleOrDefaultAsync(x => x.OrderId == order.Id && x.Status == PaymentStatus.Created, cancellationToken);
        if (payment is null)
        {
            var now = DateTime.UtcNow;
            payment = new PaymentRecord { OrderId = order.Id, PaymentNo = $"P{now:yyyyMMddHHmmssfff}{Random.Shared.Next(100000, 999999)}", Channel = "WECHAT", Status = PaymentStatus.Created, Amount = order.PayableAmount, CreatedAt = now, UpdatedAt = now };
            db.PaymentRecords.Add(payment);
            await db.SaveChangesAsync(cancellationToken);
        }
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        var preparation = await gateway.CreatePrepayAsync(payment.PaymentNo, payment.Amount, $"Easy Shop订单 {order.OrderNo}", user?.WechatOpenId, cancellationToken);
        return new(ToResponse(payment, preparation), null);
    }

    public async Task<PaymentStatusResponse?> QueryAsync(long userId, long id, CancellationToken ct)
    {
        var payment = await (from p in db.PaymentRecords join o in db.Orders on p.OrderId equals o.Id where p.Id == id && o.UserId == userId select p).SingleOrDefaultAsync(ct);
        if (payment is null) return null;
        if (payment.Status == PaymentStatus.Created)
        {
            var result = await gateway.QueryAsync(payment.PaymentNo, ct); payment.QueryAttempts++; payment.LastQueriedAt = DateTime.UtcNow;
            if (result.Status == "SUCCESS")
            {
                var callback = new PaymentCallbackRequest(payment.PaymentNo, true, result.TransactionId, string.Empty);
                callback = callback with { Signature = CreateCallbackSignature(callback) };
                await HandleCallbackAsync(callback, ct);
            }
            else if (result.Status is "CLOSED" or "REVOKED") { payment.Status = PaymentStatus.Closed; await db.SaveChangesAsync(ct); }
            else await db.SaveChangesAsync(ct);
        }
        return new(payment.Id, payment.OrderId, payment.Status, payment.ExternalTransactionNo, payment.PaidAt);
    }

    public async Task SyncByPaymentNoAsync(string paymentNo, CancellationToken ct)
    {
        var payment = await db.PaymentRecords.SingleOrDefaultAsync(x => x.PaymentNo == paymentNo && x.Status == PaymentStatus.Created, ct);
        if (payment is null) return;
        var result = await gateway.QueryAsync(paymentNo, ct); payment.QueryAttempts++; payment.LastQueriedAt = DateTime.UtcNow;
        if (result.Status == "SUCCESS") { var callback = new PaymentCallbackRequest(paymentNo, true, result.TransactionId, string.Empty); await HandleCallbackAsync(callback with { Signature = CreateCallbackSignature(callback) }, ct); }
        else if (result.Status is "CLOSED" or "REVOKED") { payment.Status = PaymentStatus.Closed; payment.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
        else await db.SaveChangesAsync(ct);
    }

    public async Task<(RefundResponse? Value, string? Error)> RefundAsync(long userId, RefundRequest request, CancellationToken ct)
    {
        var payment = await (from p in db.PaymentRecords join o in db.Orders on p.OrderId equals o.Id where p.OrderId == request.OrderId && o.UserId == userId && p.Status == PaymentStatus.Paid select p).SingleOrDefaultAsync(ct);
        if (payment is null) return (null, "没有可退款的已支付订单。");
        var amount = request.Amount ?? payment.Amount; if (amount <= 0 || amount > payment.Amount) return (null, "退款金额无效。");
        var now = DateTime.UtcNow; var item = new PaymentRefund { PaymentId = payment.Id, OrderId = payment.OrderId, RefundNo = $"R{now:yyyyMMddHHmmssfff}{Random.Shared.Next(100000,999999)}", RefundAmount = amount, CreatedAt = now, UpdatedAt = now };
        db.PaymentRefunds.Add(item); payment.Status = PaymentStatus.Refunding; payment.UpdatedAt = now; await db.SaveChangesAsync(ct);
        var result = await gateway.RefundAsync(payment.PaymentNo, item.RefundNo, amount, ct); item.Status = result.Status; item.ExternalRefundNo = result.RefundId; item.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct);
        return (new(item.Id, item.OrderId, item.RefundNo, item.RefundAmount, item.Status, item.ExternalRefundNo, item.CreatedAt), null);
    }

    public async Task<PaymentReconciliation> ReconcileAsync(DateOnly date, CancellationToken ct)
    {
        var bill = await gateway.DownloadTradeBillAsync(date, ct); var item = new PaymentReconciliation { TradeDate = date, Status = string.IsNullOrWhiteSpace(bill) ? "EMPTY" : "DOWNLOADED", CreatedAt = DateTime.UtcNow }; db.PaymentReconciliations.Add(item); await db.SaveChangesAsync(ct); return item;
    }

    public async Task<PaymentResponse?> GetAsync(long userId, long id, CancellationToken cancellationToken)
    {
        var result = await (from payment in db.PaymentRecords.AsNoTracking()
                             join order in db.Orders.AsNoTracking() on payment.OrderId equals order.Id
                             where payment.Id == id && order.UserId == userId
                             select new PaymentResponse(payment.Id, payment.OrderId, payment.PaymentNo, payment.Channel, payment.Status, payment.Amount, null, null, null, payment.CreatedAt, payment.PaidAt)).SingleOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<bool> HandleCallbackAsync(PaymentCallbackRequest request, CancellationToken cancellationToken)
    {
        if (!Verify(request)) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var payment = await db.PaymentRecords.SingleOrDefaultAsync(x => x.PaymentNo == request.PaymentNo, cancellationToken);
        if (payment is null) return false;
        if (payment.Status == PaymentStatus.Paid) return true;
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == payment.OrderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.WaitPay) return false;
        var now = DateTime.UtcNow;
        if (!request.Success)
        {
            payment.Status = PaymentStatus.Failed; payment.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return true;
        }
        foreach (var item in order.Items)
        {
            var affected = await db.ProductSkus.Where(x => x.Id == item.SkuId && x.Stock >= item.Quantity && x.LockedStock >= item.Quantity)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.Stock, s => s.Stock - item.Quantity).SetProperty(s => s.LockedStock, s => s.LockedStock - item.Quantity), cancellationToken);
            if (affected != 1) { await transaction.RollbackAsync(cancellationToken); return false; }
        }
        payment.Status = PaymentStatus.Paid; payment.ExternalTransactionNo = request.ExternalTransactionNo; payment.PaidAt = now; payment.UpdatedAt = now;
        order.Status = OrderStatus.Paid; order.PaidAt = now; order.UpdatedAt = now;
        foreach (var grouped in order.Items.GroupBy(x => x.ProductId)) await db.Products.Where(x => x.Id == grouped.Key).ExecuteUpdateAsync(x => x.SetProperty(p => p.SalesCount, p => p.SalesCount + grouped.Sum(i => i.Quantity)).SetProperty(p => p.UpdatedAt, now), cancellationToken);
        var earnedPoints = (int)Math.Floor(order.PayableAmount);
        if (earnedPoints > 0)
        {
            var account = await db.PointsAccounts.SingleOrDefaultAsync(x => x.UserId == order.UserId, cancellationToken);
            if (account is null) { account = new PointsAccount { UserId = order.UserId, Balance = earnedPoints, UpdatedAt = now }; db.PointsAccounts.Add(account); }
            else { account.Balance += earnedPoints; account.UpdatedAt = now; }
            db.PointsTransactions.Add(new PointsTransaction { UserId = order.UserId, Amount = earnedPoints, BalanceAfter = account.Balance, Type = "ORDER_EARN", OrderId = order.Id, CreatedAt = now });
        }
        var promotionOrder = await db.PromotionOrders.SingleOrDefaultAsync(x => x.OrderId == order.Id, cancellationToken);
        if (promotionOrder is not null) { promotionOrder.Status = PromotionOrderStatus.Paid; promotionOrder.UpdatedAt = now; }
        var buyer = await db.Distributors.SingleOrDefaultAsync(x => x.UserId == order.UserId && x.Enabled && x.ParentDistributorId != null, cancellationToken);
        if (buyer?.ParentDistributorId is long parentId && !await db.DistributionCommissions.AnyAsync(x => x.OrderId == order.Id, cancellationToken))
        {
            var parent = await db.Distributors.SingleOrDefaultAsync(x => x.Id == parentId && x.Enabled, cancellationToken);
            if (parent is not null)
                db.DistributionCommissions.Add(new DistributionCommission { DistributorId = parent.Id, OrderId = order.Id, Rate = parent.CommissionRate, Amount = order.PayableAmount * parent.CommissionRate, Status = "PENDING", CreatedAt = now });
        }
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return true;
    }

    public string CreateCallbackSignature(PaymentCallbackRequest request) => Sign(request);

    public async Task<bool> HandleWechatCallbackAsync(string body, string timestamp, string nonce, string signature, CancellationToken ct)
    {
        if (!VerifyWechatSignature(body, timestamp, nonce, signature)) return false;
        using var document = JsonDocument.Parse(body); var resource = document.RootElement.GetProperty("resource");
        var cipher = Convert.FromBase64String(resource.GetProperty("ciphertext").GetString()!); var nonceBytes = Encoding.UTF8.GetBytes(resource.GetProperty("nonce").GetString()!); var aad = Encoding.UTF8.GetBytes(resource.GetProperty("associated_data").GetString()!);
        var plain = new byte[cipher.Length - 16]; using var aes = new System.Security.Cryptography.AesGcm(Encoding.UTF8.GetBytes(_options.ApiV3Key), 16); aes.Decrypt(nonceBytes, cipher[..^16], cipher[^16..], plain, aad);
        using var payload = JsonDocument.Parse(Encoding.UTF8.GetString(plain)); var root = payload.RootElement; var state = root.GetProperty("trade_state").GetString(); var paymentNo = root.GetProperty("out_trade_no").GetString()!;
        var request = new PaymentCallbackRequest(paymentNo, state == "SUCCESS", root.TryGetProperty("transaction_id", out var transaction) ? transaction.GetString() : null, string.Empty); return await HandleCallbackAsync(request with { Signature = CreateCallbackSignature(request) }, ct);
    }

    private bool VerifyWechatSignature(string body, string timestamp, string nonce, string signature)
    {
        if (string.IsNullOrWhiteSpace(_options.PlatformCertificatePem)) return false;
        using var cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(_options.PlatformCertificatePem); using var rsa = cert.GetRSAPublicKey();
        return rsa is not null && rsa.VerifyData(Encoding.UTF8.GetBytes($"{timestamp}\n{nonce}\n{body}\n"), Convert.FromBase64String(signature), System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
    }

    private bool Verify(PaymentCallbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.CallbackSecret) || string.IsNullOrWhiteSpace(request.Signature)) return false;
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Sign(request)), Convert.FromHexString(request.Signature)); }
        catch (FormatException) { return false; }
    }

    private string Sign(PaymentCallbackRequest request)
    {
        var value = $"{request.PaymentNo}|{request.Success}|{request.ExternalTransactionNo ?? string.Empty}";
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.CallbackSecret), Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static PaymentResponse ToResponse(PaymentRecord x, PaymentPreparation p) => new(x.Id, x.OrderId, x.PaymentNo, x.Channel, x.Status, x.Amount, p.PrepayId, p.CodeUrl, p.Package, x.CreatedAt, x.PaidAt);
}
