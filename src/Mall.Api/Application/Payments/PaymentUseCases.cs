using System.Security.Cryptography;
using System.Text;
using Mall.Api.Domain.Orders;
using Mall.Api.Domain.Payments;
using Mall.Api.Domain.Promotions;
using Mall.Api.Infrastructure;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
        var prepayId = await gateway.CreatePrepayAsync(payment.PaymentNo, payment.Amount, cancellationToken);
        return new(ToResponse(payment, prepayId), null);
    }

    public async Task<PaymentResponse?> GetAsync(long userId, long id, CancellationToken cancellationToken)
    {
        var result = await (from payment in db.PaymentRecords.AsNoTracking()
                             join order in db.Orders.AsNoTracking() on payment.OrderId equals order.Id
                             where payment.Id == id && order.UserId == userId
                             select new PaymentResponse(payment.Id, payment.OrderId, payment.PaymentNo, payment.Channel, payment.Status, payment.Amount, null, payment.CreatedAt, payment.PaidAt)).SingleOrDefaultAsync(cancellationToken);
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

    private static PaymentResponse ToResponse(PaymentRecord x, string? prepayId) => new(x.Id, x.OrderId, x.PaymentNo, x.Channel, x.Status, x.Amount, prepayId, x.CreatedAt, x.PaidAt);
}
