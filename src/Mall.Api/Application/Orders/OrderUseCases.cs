using Mall.Api.Domain.Orders;
using Mall.Api.Domain.Promotions;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Orders;

public sealed class OrderUseCases(MallDbContext db)
{
    public async Task<OrderCreateResult> CreateAsync(long userId, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var address = await db.UserAddresses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.AddressId && x.UserId == userId, cancellationToken);
        if (address is null) return new(null, "收货地址不存在。");

        var selectedIds = request.CartItemIds is { Length: > 0 } ? request.CartItemIds : null;
        var lines = await (
            from cart in db.CartItems
            join sku in db.ProductSkus on cart.SkuId equals sku.Id
            join product in db.Products on cart.ProductId equals product.Id
            where cart.UserId == userId && cart.Checked && (selectedIds == null || selectedIds.Contains(cart.Id))
            select new OrderLine(cart.Id, product.Id, product.Name, product.IsOnSale, sku.Id, sku.SkuCode, sku.Price, cart.Quantity, sku.Stock, sku.LockedStock)
        ).ToArrayAsync(cancellationToken);
        if (lines.Length == 0) return new(null, "没有可提交的购物车商品。");
        if (lines.Any(x => !x.IsOnSale || x.Quantity <= 0)) return new(null, "购物车中存在已下架商品。");
        var now = DateTime.UtcNow;
        GroupBuyActivity? groupActivity = null;
        if (request.GroupBuyActivityId is not null)
        {
            groupActivity = await db.GroupBuyActivities.SingleOrDefaultAsync(x => x.Id == request.GroupBuyActivityId && x.Enabled && x.StartsAt <= now && x.EndsAt > now, cancellationToken);
            if (groupActivity is null || lines.Length != 1 || lines[0].SkuId != groupActivity.SkuId) return new(null, "拼团活动与商品不匹配。");
            if (request.GroupBuyTeamId is not null && !await db.GroupBuyTeams.AnyAsync(x => x.Id == request.GroupBuyTeamId && x.ActivityId == groupActivity.Id && x.Status == GroupBuyTeamStatus.Open && x.ExpiresAt > now, cancellationToken)) return new(null, "拼团团队无效。");
            lines = lines.Select(x => x with { Price = request.GroupBuyTeamId is null ? groupActivity.LeaderPrice : groupActivity.MemberPrice }).ToArray();
        }
        else if (request.GroupBuyTeamId is not null) return new(null, "缺少拼团活动。");
        FlashSaleActivity? flashActivity = null;
        if (request.FlashSaleActivityId is not null)
        {
            flashActivity = await db.FlashSaleActivities.SingleOrDefaultAsync(x => x.Id == request.FlashSaleActivityId && x.Enabled && x.StartsAt <= now && x.EndsAt > now, cancellationToken);
            if (flashActivity is null || lines.Length != 1 || lines[0].SkuId != flashActivity.SkuId || lines[0].Quantity > flashActivity.PerUserLimit) return new(null, "秒杀活动与商品不匹配或超过限购。");
            if (await db.FlashSaleOrders.AnyAsync(x => x.ActivityId == flashActivity.Id && x.UserId == userId, cancellationToken)) return new(null, "用户已参与该秒杀。");
            lines = lines.Select(x => x with { Price = flashActivity.SalePrice }).ToArray();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var line in lines)
        {
            var affected = await db.ProductSkus
                .Where(x => x.Id == line.SkuId && x.Enabled && x.Stock - x.LockedStock >= line.Quantity)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.LockedStock, s => s.LockedStock + line.Quantity), cancellationToken);
            if (affected != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(null, $"商品 {line.ProductName} 库存不足。");
            }
        }

        var goodsAmount = lines.Sum(x => x.Price * x.Quantity);
        var order = new Order
        {
            OrderNo = $"{now:yyyyMMddHHmmssfff}{Random.Shared.Next(100000, 999999)}",
            UserId = userId,
            Status = OrderStatus.WaitPay,
            GoodsAmount = goodsAmount,
            PayableAmount = goodsAmount,
            Consignee = address.Consignee,
            Mobile = address.Mobile,
            Address = $"{address.Province}{address.City}{address.District}{address.Detail}",
            CreatedAt = now,
            UpdatedAt = now,
            Items = lines.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                SkuId = x.SkuId,
                ProductName = x.ProductName,
                SkuCode = x.SkuCode,
                UnitPrice = x.Price,
                Quantity = x.Quantity,
                TotalAmount = x.Price * x.Quantity
            }).ToList()
        };
        db.Orders.Add(order);
        db.CartItems.RemoveRange(await db.CartItems.Where(x => lines.Select(l => l.CartItemId).Contains(x.Id)).ToArrayAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);

        if (flashActivity is not null)
        {
            var updated = await db.FlashSaleActivities.Where(x => x.Id == flashActivity.Id && x.SoldQuantity + lines[0].Quantity <= x.TotalStock)
                .ExecuteUpdateAsync(x => x.SetProperty(a => a.SoldQuantity, a => a.SoldQuantity + lines[0].Quantity), cancellationToken);
            if (updated != 1) return new(null, "秒杀库存不足。");
            db.FlashSaleOrders.Add(new FlashSaleOrder { ActivityId = flashActivity.Id, UserId = userId, OrderId = order.Id, Quantity = lines[0].Quantity, CreatedAt = now });
        }
        if (groupActivity is not null)
        {
            GroupBuyTeam team;
            if (request.GroupBuyTeamId is null)
            {
                team = new GroupBuyTeam { ActivityId = groupActivity.Id, LeaderUserId = userId, CurrentMembers = 1, Status = GroupBuyTeamStatus.Open, ExpiresAt = now.AddHours(24), CreatedAt = now };
                db.GroupBuyTeams.Add(team); await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                team = await db.GroupBuyTeams.SingleAsync(x => x.Id == request.GroupBuyTeamId.Value, cancellationToken);
                var updated = await db.GroupBuyTeams.Where(x => x.Id == team.Id && x.CurrentMembers < groupActivity.RequiredMembers)
                    .ExecuteUpdateAsync(x => x.SetProperty(t => t.CurrentMembers, t => t.CurrentMembers + 1), cancellationToken);
                if (updated != 1) return new(null, "拼团已满。");
                team.CurrentMembers++;
            }
            db.GroupBuyMembers.Add(new GroupBuyMember { TeamId = team.Id, UserId = userId, OrderId = order.Id, CreatedAt = now });
            if (team.CurrentMembers >= groupActivity.RequiredMembers) team.Status = GroupBuyTeamStatus.Succeeded;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(ToResponse(order), null);
    }

    public async Task<OrderListResponse> ListAsync(long userId, OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Clamp(page, 1, 100_000); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Orders.AsNoTracking().Where(x => x.UserId == userId);
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync(cancellationToken);
        var orders = await query.Include(x => x.Items).OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return new(page, pageSize, total, orders.Select(ToResponse).ToArray());
    }

    public async Task<OrderResponse?> GetAsync(long userId, long id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        return order is null ? null : ToResponse(order);
    }

    public async Task<bool> CancelAsync(long userId, long id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (order is null || order.Status != OrderStatus.WaitPay) return false;
        foreach (var item in order.Items)
            await db.ProductSkus.Where(x => x.Id == item.SkuId && x.LockedStock >= item.Quantity).ExecuteUpdateAsync(x => x.SetProperty(s => s.LockedStock, s => s.LockedStock - item.Quantity), cancellationToken);
        order.Status = OrderStatus.Cancelled; order.CancelledAt = DateTime.UtcNow; order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> FinishAsync(long userId, long id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Status == OrderStatus.Shipped, cancellationToken);
        if (order is null) return false;
        order.Status = OrderStatus.Finished; order.FinishedAt = DateTime.UtcNow; order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static OrderResponse ToResponse(Order order) => new(order.Id, order.OrderNo, order.Status, order.GoodsAmount, order.FreightAmount, order.DiscountAmount, order.PayableAmount, order.Consignee, order.Mobile, order.Address, order.CreatedAt, order.PaidAt, order.ShippedAt, order.FinishedAt, order.Items.Select(x => new OrderItemResponse(x.Id, x.ProductId, x.SkuId, x.ProductName, x.SkuCode, x.UnitPrice, x.Quantity, x.TotalAmount)).ToArray());
    private sealed record OrderLine(long CartItemId, long ProductId, string ProductName, bool IsOnSale, long SkuId, string SkuCode, decimal Price, int Quantity, int Stock, int LockedStock);
}
