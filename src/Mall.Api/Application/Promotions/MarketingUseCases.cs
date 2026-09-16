using Mall.Api.Domain.Promotions;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Promotions;

public sealed class MarketingUseCases(MallDbContext db)
{
    public async Task<CouponResponse[]> CouponsAsync(long userId, CancellationToken ct) => await (from x in db.UserCoupons.AsNoTracking() join c in db.Coupons.AsNoTracking() on x.CouponId equals c.Id where x.UserId == userId select new CouponResponse(x.Id, c.Id, c.Name, c.Code, c.ThresholdAmount, c.DiscountAmount, x.Status, c.StartsAt, c.EndsAt)).ToArrayAsync(ct);

    public async Task<(CouponResponse? Value, string? Error)> ClaimCouponAsync(long userId, string code, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var coupon = await db.Coupons.SingleOrDefaultAsync(x => x.Code == code && x.Enabled && x.StartsAt <= now && x.EndsAt > now && x.IssuedQuantity < x.TotalQuantity, ct);
        if (coupon is null) return (null, "优惠券不存在或已领完。");
        if (await db.UserCoupons.CountAsync(x => x.CouponId == coupon.Id && x.UserId == userId, ct) >= coupon.PerUserLimit) return (null, "已达到领取上限。");
        coupon.IssuedQuantity++;
        var item = new UserCoupon { CouponId = coupon.Id, UserId = userId, ReceivedAt = now };
        db.UserCoupons.Add(item); await db.SaveChangesAsync(ct);
        return (new(item.Id, coupon.Id, coupon.Name, coupon.Code, coupon.ThresholdAmount, coupon.DiscountAmount, item.Status, coupon.StartsAt, coupon.EndsAt), null);
    }

    public async Task<PointsResponse> PointsAsync(long userId, CancellationToken ct) => new(userId, await db.PointsAccounts.Where(x => x.UserId == userId).Select(x => x.Balance).SingleOrDefaultAsync(ct));

    public async Task<ActivityDetailResponse?> ActivityDetailAsync(string type, long id, CancellationToken ct)
    {
        if (type.Equals("group-buy", StringComparison.OrdinalIgnoreCase)) return await (from x in db.GroupBuyActivities join p in db.Products on x.ProductId equals p.Id where x.Id == id select new ActivityDetailResponse(x.Id, type, p.Name, x.StartsAt, x.EndsAt, $"拼团价：{x.MemberPrice:0.00}，需 {x.RequiredMembers} 人")).SingleOrDefaultAsync(ct);
        if (type.Equals("flash-sale", StringComparison.OrdinalIgnoreCase)) return await (from x in db.FlashSaleActivities join p in db.Products on x.ProductId equals p.Id where x.Id == id select new ActivityDetailResponse(x.Id, type, p.Name, x.StartsAt, x.EndsAt, $"限时价：{x.SalePrice:0.00}，剩余 {x.TotalStock - x.SoldQuantity}")).SingleOrDefaultAsync(ct);
        if (type.Equals("bargain", StringComparison.OrdinalIgnoreCase)) return await (from x in db.BargainActivities join p in db.Products on x.ProductId equals p.Id where x.Id == id select new ActivityDetailResponse(x.Id, type, p.Name, x.StartsAt, x.EndsAt, $"最低价：{x.LowestPrice:0.00}，需 {x.RequiredHelpers} 位好友助力")).SingleOrDefaultAsync(ct);
        if (type.Equals("presale", StringComparison.OrdinalIgnoreCase)) return await (from x in db.PresaleActivities join p in db.Products on x.ProductId equals p.Id where x.Id == id select new ActivityDetailResponse(x.Id, type, p.Name, x.StartsAt, x.EndsAt, $"定金：{x.DepositAmount:0.00}，尾款：{x.FinalAmount:0.00}")).SingleOrDefaultAsync(ct);
        return null;
    }
    public async Task<PromotionOrderResponse[]> PromotionOrdersAsync(long userId, CancellationToken ct) => await db.PromotionOrders.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Select(x => new PromotionOrderResponse(x.Id, x.OrderId, x.Type, x.ActivityId, x.Status, x.UpdatedAt)).ToArrayAsync(ct);

    public async Task<(BargainResponse? Value, string? Error)> StartBargainAsync(long userId, long activityId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var activity = await db.BargainActivities.SingleOrDefaultAsync(x => x.Id == activityId && x.Enabled && x.StartsAt <= now && x.EndsAt > now, ct);
        if (activity is null) return (null, "砍价活动不存在或已结束。");
        if (await db.BargainRecords.AnyAsync(x => x.ActivityId == activityId && x.UserId == userId, ct)) return (null, "已参加该砍价活动。");
        var record = new BargainRecord { ActivityId = activityId, UserId = userId, CurrentPrice = activity.OriginalPrice, CreatedAt = now, ExpiresAt = now.AddHours(24) };
        db.BargainRecords.Add(record); await db.SaveChangesAsync(ct); return (new(record.Id, record.ActivityId, record.CurrentPrice, record.HelperCount, record.Status, record.ExpiresAt), null);
    }

    public async Task<(BargainResponse? Value, string? Error)> HelpBargainAsync(long userId, long recordId, CancellationToken ct)
    {
        var record = await db.BargainRecords.SingleOrDefaultAsync(x => x.Id == recordId && x.Status == "ONGOING" && x.ExpiresAt > DateTime.UtcNow, ct);
        if (record is null || record.UserId == userId) return (null, "砍价记录无效。");
        var activity = await db.BargainActivities.SingleAsync(x => x.Id == record.ActivityId, ct);
        if (record.HelperCount >= activity.RequiredHelpers) return (null, "砍价已完成。");
        record.HelperCount++; record.CurrentPrice = Math.Max(activity.LowestPrice, record.CurrentPrice - activity.StepAmount);
        if (record.CurrentPrice <= activity.LowestPrice) record.Status = "SUCCESS";
        await db.SaveChangesAsync(ct); return (new(record.Id, record.ActivityId, record.CurrentPrice, record.HelperCount, record.Status, record.ExpiresAt), null);
    }
}

public sealed record CouponResponse(long Id, long CouponId, string Name, string Code, decimal ThresholdAmount, decimal DiscountAmount, string Status, DateTime StartsAt, DateTime EndsAt);
public sealed record PointsResponse(long UserId, int Balance);
public sealed record ActivityDetailResponse(long Id, string Type, string ProductName, DateTime StartsAt, DateTime EndsAt, string Rule);
public sealed record BargainResponse(long Id, long ActivityId, decimal CurrentPrice, int HelperCount, string Status, DateTime ExpiresAt);
public sealed record PromotionOrderResponse(long Id, long OrderId, PromotionActivityType Type, long? ActivityId, PromotionOrderStatus Status, DateTime UpdatedAt);
