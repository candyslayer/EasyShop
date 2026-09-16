using System.Text.Json;
using Mall.Api.Domain.Orders;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Orders;

public sealed class OrderExtraUseCases(MallDbContext db)
{
    public async Task<(AfterSaleResponse? Value, string? Error)> ApplyAfterSaleAsync(long userId, AfterSaleRequest request, CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == request.OrderId && x.UserId == userId && (x.Status == OrderStatus.Paid || x.Status == OrderStatus.Shipped || x.Status == OrderStatus.Finished), ct);
        if (order is null) return (null, "订单不存在或不满足售后条件。");
        if (await db.AfterSales.AnyAsync(x => x.OrderId == request.OrderId && x.Status != AfterSaleStatus.Rejected && x.Status != AfterSaleStatus.Closed, ct)) return (null, "该订单已有进行中的售后申请。");
        var item = new AfterSale { OrderId = order.Id, UserId = userId, Type = request.Type, Amount = Math.Min(request.Amount ?? order.PayableAmount, order.PayableAmount), Reason = request.Reason, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; db.AfterSales.Add(item); await db.SaveChangesAsync(ct); return (ToResponse(item), null);
    }
    public async Task<AfterSaleResponse[]> AfterSalesAsync(long userId, CancellationToken ct) => await db.AfterSales.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Select(x => new AfterSaleResponse(x.Id, x.OrderId, x.Type, x.Amount, x.Reason, x.Status, x.CreatedAt, x.UpdatedAt)).ToArrayAsync(ct);
    public async Task<LogisticsResponse?> LogisticsAsync(long userId, long orderId, CancellationToken ct) => await (from t in db.LogisticsTraces.AsNoTracking() join o in db.Orders.AsNoTracking() on t.OrderId equals o.Id where t.OrderId == orderId && o.UserId == userId select new LogisticsResponse(t.OrderId, t.Company, t.TrackingNo, t.Status, t.TraceJson, t.UpdatedAt)).SingleOrDefaultAsync(ct);
    public async Task<(OrderReviewResponse? Value, string? Error)> ReviewAsync(long userId, long orderId, ReviewRequest request, CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5) return (null, "评分必须为 1-5 分。"); var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == orderId && x.UserId == userId && x.Status == OrderStatus.Finished, ct); if (order is null) return (null, "只有已完成订单可以评价。"); if (await db.OrderReviews.AnyAsync(x => x.OrderId == orderId && x.UserId == userId, ct)) return (null, "订单已评价。"); var review = new OrderReview { OrderId = orderId, UserId = userId, Rating = request.Rating, Content = request.Content?.Trim() ?? string.Empty, Anonymous = request.Anonymous, CreatedAt = DateTime.UtcNow }; db.OrderReviews.Add(review); await db.SaveChangesAsync(ct); return (new(review.Id, review.OrderId, review.Rating, review.Content, review.Anonymous, review.CreatedAt), null);
    }
    private static AfterSaleResponse ToResponse(AfterSale x) => new(x.Id, x.OrderId, x.Type, x.Amount, x.Reason, x.Status, x.CreatedAt, x.UpdatedAt);
}
public sealed record AfterSaleRequest(long OrderId, string Type, decimal? Amount, string Reason);
public sealed record AfterSaleResponse(long Id, long OrderId, string Type, decimal Amount, string Reason, AfterSaleStatus Status, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record LogisticsResponse(long OrderId, string Company, string TrackingNo, string Status, string TraceJson, DateTime UpdatedAt);
public sealed record ReviewRequest(int Rating, string? Content = null, bool Anonymous = false);
public sealed record OrderReviewResponse(long Id, long OrderId, int Rating, string Content, bool Anonymous, DateTime CreatedAt);
