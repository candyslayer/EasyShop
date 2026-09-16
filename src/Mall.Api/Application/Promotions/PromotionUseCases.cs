using Mall.Api.Domain.Orders;
using Mall.Api.Domain.Promotions;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Promotions;

public sealed class PromotionUseCases(MallDbContext db)
{
    public async Task<GroupBuyActivityResponse[]> GroupBuyActivitiesAsync(CancellationToken ct) =>
        await (from a in db.GroupBuyActivities.AsNoTracking()
               join p in db.Products.AsNoTracking() on a.ProductId equals p.Id
               where a.Enabled && a.StartsAt <= DateTime.UtcNow && a.EndsAt > DateTime.UtcNow
               select new GroupBuyActivityResponse(a.Id, a.ProductId, a.SkuId, p.Name, a.LeaderPrice, a.MemberPrice, a.RequiredMembers, a.StartsAt, a.EndsAt)).ToArrayAsync(ct);

    public async Task<GroupBuyTeamResponse[]> TeamsAsync(long activityId, CancellationToken ct) =>
        await (from t in db.GroupBuyTeams.AsNoTracking()
               join a in db.GroupBuyActivities.AsNoTracking() on t.ActivityId equals a.Id
               where t.ActivityId == activityId && t.Status == GroupBuyTeamStatus.Open && t.ExpiresAt > DateTime.UtcNow
               select new GroupBuyTeamResponse(t.Id, t.ActivityId, t.LeaderUserId, t.CurrentMembers, a.RequiredMembers, t.Status, t.ExpiresAt)).ToArrayAsync(ct);

    public async Task<(GroupBuyTeamResponse? Value, string? Error)> CreateTeamAsync(long userId, CreateGroupBuyTeamRequest request, CancellationToken ct)
    {
        var activity = await db.GroupBuyActivities.SingleOrDefaultAsync(x => x.Id == request.ActivityId && x.Enabled && x.StartsAt <= DateTime.UtcNow && x.EndsAt > DateTime.UtcNow, ct);
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == request.OrderId && x.UserId == userId && x.Status == OrderStatus.WaitPay, ct);
        if (activity is null || order is null) return (null, "拼团活动或订单无效。");
        var now = DateTime.UtcNow;
        var team = new GroupBuyTeam { ActivityId = activity.Id, LeaderUserId = userId, CurrentMembers = 1, Status = GroupBuyTeamStatus.Open, ExpiresAt = now.AddHours(24), CreatedAt = now };
        db.GroupBuyTeams.Add(team); await db.SaveChangesAsync(ct);
        db.GroupBuyMembers.Add(new GroupBuyMember { TeamId = team.Id, UserId = userId, OrderId = order.Id, CreatedAt = now }); await db.SaveChangesAsync(ct);
        return (new GroupBuyTeamResponse(team.Id, team.ActivityId, team.LeaderUserId, 1, activity.RequiredMembers, team.Status, team.ExpiresAt), null);
    }

    public async Task<(GroupBuyTeamResponse? Value, string? Error)> JoinTeamAsync(long userId, long teamId, JoinGroupBuyTeamRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var team = await db.GroupBuyTeams.SingleOrDefaultAsync(x => x.Id == teamId && x.Status == GroupBuyTeamStatus.Open && x.ExpiresAt > DateTime.UtcNow, ct);
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == request.OrderId && x.UserId == userId && x.Status == OrderStatus.WaitPay, ct);
        if (team is null || order is null) return (null, "拼团或订单无效。");
        if (await db.GroupBuyMembers.AnyAsync(x => x.TeamId == teamId && x.UserId == userId, ct)) return (null, "用户已加入该拼团。");
        var affected = await db.GroupBuyTeams.Where(x => x.Id == teamId && x.CurrentMembers < db.GroupBuyActivities.Where(a => a.Id == x.ActivityId).Select(a => a.RequiredMembers).First()).ExecuteUpdateAsync(x => x.SetProperty(t => t.CurrentMembers, t => t.CurrentMembers + 1), ct);
        if (affected != 1) return (null, "拼团已满。");
        db.GroupBuyMembers.Add(new GroupBuyMember { TeamId = teamId, UserId = userId, OrderId = request.OrderId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        var result = await (from t in db.GroupBuyTeams.AsNoTracking() join a in db.GroupBuyActivities.AsNoTracking() on t.ActivityId equals a.Id where t.Id == teamId select new GroupBuyTeamResponse(t.Id, t.ActivityId, t.LeaderUserId, t.CurrentMembers, a.RequiredMembers, t.Status, t.ExpiresAt)).SingleAsync(ct);
        return (result, null);
    }

    public async Task<FlashSaleResponse[]> FlashSalesAsync(CancellationToken ct) =>
        await (from a in db.FlashSaleActivities.AsNoTracking() join p in db.Products.AsNoTracking() on a.ProductId equals p.Id where a.Enabled && a.StartsAt <= DateTime.UtcNow && a.EndsAt > DateTime.UtcNow select new FlashSaleResponse(a.Id, a.ProductId, a.SkuId, p.Name, a.SalePrice, a.TotalStock - a.SoldQuantity, a.PerUserLimit, a.StartsAt, a.EndsAt)).ToArrayAsync(ct);

    public async Task<string?> ReserveFlashSaleAsync(long userId, FlashSaleReservationRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0) return "数量无效。";
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var activity = await db.FlashSaleActivities.SingleOrDefaultAsync(x => x.Id == request.ActivityId && x.Enabled && x.StartsAt <= DateTime.UtcNow && x.EndsAt > DateTime.UtcNow, ct);
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == request.OrderId && x.UserId == userId && x.Status == OrderStatus.WaitPay, ct);
        if (activity is null || order is null) return "秒杀活动或订单无效。";
        if (await db.FlashSaleOrders.AnyAsync(x => x.ActivityId == activity.Id && x.UserId == userId, ct)) return "用户已参与该秒杀。";
        if (request.Quantity > activity.PerUserLimit) return "超过个人限购数量。";
        if (await db.FlashSaleActivities.Where(x => x.Id == activity.Id && x.SoldQuantity + request.Quantity <= x.TotalStock).ExecuteUpdateAsync(x => x.SetProperty(a => a.SoldQuantity, a => a.SoldQuantity + request.Quantity), ct) != 1) return "秒杀库存不足。";
        db.FlashSaleOrders.Add(new FlashSaleOrder { ActivityId = activity.Id, UserId = userId, OrderId = order.Id, Quantity = request.Quantity, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return null;
    }

    public async Task<DistributorResponse> ActivateDistributorAsync(long userId, CancellationToken ct)
    {
        var current = await db.Distributors.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (current is not null) return new(current.Id, current.InviteCode, current.ParentDistributorId, current.CommissionRate, current.Enabled);
        var item = new Distributor { UserId = userId, InviteCode = $"U{userId}{Random.Shared.Next(100000, 999999)}", CommissionRate = 0.05m, CreatedAt = DateTime.UtcNow };
        db.Distributors.Add(item); await db.SaveChangesAsync(ct); return new(item.Id, item.InviteCode, item.ParentDistributorId, item.CommissionRate, item.Enabled);
    }

    public async Task<string?> BindDistributorAsync(long userId, BindDistributorRequest request, CancellationToken ct)
    {
        var parent = await db.Distributors.SingleOrDefaultAsync(x => x.InviteCode == request.InviteCode && x.Enabled, ct);
        if (parent is null) return "邀请码无效。";
        var current = await db.Distributors.SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? new Distributor { UserId = userId, InviteCode = $"U{userId}{Random.Shared.Next(100000, 999999)}", CommissionRate = 0.05m, CreatedAt = DateTime.UtcNow };
        if (current.Id == parent.Id) return "不能绑定自己。";
        current.ParentDistributorId = parent.Id; if (current.Id == 0) db.Distributors.Add(current); await db.SaveChangesAsync(ct); return null;
    }
}
