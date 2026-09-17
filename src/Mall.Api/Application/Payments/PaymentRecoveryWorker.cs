using Mall.Api.Domain.Payments;
using Mall.Api.Infrastructure;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mall.Api.Application.Payments;

public sealed class PaymentRecoveryWorker(IServiceScopeFactory scopes, IOptions<PaymentOptions> options, ILogger<PaymentRecoveryWorker> logger) : BackgroundService
{
    private readonly PaymentOptions config = options.Value;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MallDbContext>(); var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>(); var paymentUseCases = scope.ServiceProvider.GetRequiredService<PaymentUseCases>(); var now = DateTime.UtcNow;
                var payments = await db.PaymentRecords.Where(x => x.Status == PaymentStatus.Created && (x.LastQueriedAt == null || x.LastQueriedAt < now.AddMinutes(-2))).Take(100).ToArrayAsync(stoppingToken);
                foreach (var payment in payments)
                {
                    if (payment.CreatedAt < now.AddMinutes(-config.PaymentTimeoutMinutes)) 
                        { 
                            await gateway.CloseAsync(payment.PaymentNo, stoppingToken); 
                            payment.Status = PaymentStatus.Closed; 
                            payment.UpdatedAt = now; continue; 
                        }
                    try
                        { 
                            var state = await gateway.QueryAsync(payment.PaymentNo, stoppingToken); 
                            payment.QueryAttempts++; 
                            payment.LastQueriedAt = now; 
                            if (state.Status == "SUCCESS") 
                                await paymentUseCases.SyncByPaymentNoAsync(payment.PaymentNo, stoppingToken); 
                            else { 
                                if (state.Status == "CLOSED") 
                                    payment.Status = PaymentStatus.Closed; 
                                else if (state.Status == "PAYERROR") 
                                    payment.Status = PaymentStatus.Failed; 
                                payment.UpdatedAt = now;
                            } 
                        }
                    catch (Exception ex)
                        { 
                            payment.Status = PaymentStatus.Unknown; 
                            payment.LastQueriedAt = now; 
                            payment.UpdatedAt = now; 
                            logger.LogWarning(ex, "Payment recovery query failed for {PaymentNo}", 
                            payment.PaymentNo);
                        }
                }
                var expired = await db.Orders.Include(x => x.Items).Where(x => x.Status == Mall.Api.Domain.Orders.OrderStatus.WaitPay && x.PaymentExpiredAt != null && x.PaymentExpiredAt < now).Take(100).ToArrayAsync(stoppingToken);
                foreach (var order in expired)
                {
                    foreach (var line in order.Items) await db.ProductSkus.Where(x => x.Id == line.SkuId && x.LockedStock >= line.Quantity).ExecuteUpdateAsync(x => x.SetProperty(s => s.LockedStock, s => s.LockedStock - line.Quantity), stoppingToken);
                    var coupon = await db.UserCoupons.SingleOrDefaultAsync(x => x.OrderId == order.Id && x.Status == "USED", stoppingToken); if (coupon is not null) { coupon.Status = "AVAILABLE"; coupon.OrderId = null; }
                    var points = await db.PointsTransactions.SingleOrDefaultAsync(x => x.OrderId == order.Id && x.Type == "ORDER_USE", stoppingToken); if (points is not null) { var account = await db.PointsAccounts.SingleAsync(x => x.UserId == order.UserId, stoppingToken); account.Balance -= points.Amount; db.PointsTransactions.Add(new Mall.Api.Domain.Promotions.PointsTransaction { UserId = order.UserId, Amount = -points.Amount, BalanceAfter = account.Balance, Type = "ORDER_TIMEOUT_RELEASE", OrderId = order.Id, CreatedAt = now }); }
                    order.Status = Mall.Api.Domain.Orders.OrderStatus.Cancelled; order.CancelledAt = now; order.UpdatedAt = now;
                }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Payment recovery cycle failed"); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
