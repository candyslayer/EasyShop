using Mall.Api.Domain.Cart;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Cart;

public sealed class CartUseCases(MallDbContext db)
{
    public async Task<CartResponse> GetAsync(long userId, CancellationToken cancellationToken)
    {
        var items = await Query(userId).OrderByDescending(x => x.Id).ToArrayAsync(cancellationToken);
        var checkedItems = items.Where(x => x.Checked).ToArray();
        return new CartResponse(items, checkedItems.Length, checkedItems.Sum(x => x.TotalAmount));
    }

    public async Task<CartItemResponse?> AddAsync(long userId, AddCartItemRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0) return null;
        var sku = await db.ProductSkus.AsNoTracking()
            .Where(x => x.Id == request.SkuId && x.Enabled && x.Product != null && x.Product.IsOnSale)
            .Select(x => new { x.Id, x.ProductId, AvailableStock = x.Stock - x.LockedStock })
            .SingleOrDefaultAsync(cancellationToken);
        if (sku is null) return null;

        var item = await db.CartItems.SingleOrDefaultAsync(x => x.UserId == userId && x.SkuId == request.SkuId, cancellationToken);
        var quantity = (item?.Quantity ?? 0) + request.Quantity;
        if (quantity > sku.AvailableStock) return null;
        var now = DateTime.UtcNow;
        if (item is null)
        {
            item = new CartItem { UserId = userId, ProductId = sku.ProductId, SkuId = sku.Id, Quantity = quantity, Checked = request.Checked, CreatedAt = now, UpdatedAt = now };
            db.CartItems.Add(item);
        }
        else
        {
            item.Quantity = quantity;
            item.Checked = request.Checked;
            item.UpdatedAt = now;
        }
        await db.SaveChangesAsync(cancellationToken);
        return await Query(userId).SingleAsync(x => x.Id == item.Id, cancellationToken);
    }

    public async Task<CartItemResponse?> UpdateAsync(long userId, long id, UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0) return null;
        var item = await db.CartItems.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (item is null) return null;
        var available = await db.ProductSkus.Where(x => x.Id == item.SkuId && x.Enabled).Select(x => x.Stock - x.LockedStock).SingleOrDefaultAsync(cancellationToken);
        if (request.Quantity > available) return null;
        item.Quantity = request.Quantity;
        if (request.Checked is not null) item.Checked = request.Checked.Value;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await Query(userId).SingleAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(long userId, long id, CancellationToken cancellationToken)
    {
        var item = await db.CartItems.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (item is null) return false;
        db.CartItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<CartItemResponse> Query(long userId) =>
        from cart in db.CartItems.AsNoTracking()
        join sku in db.ProductSkus.AsNoTracking() on cart.SkuId equals sku.Id
        join product in db.Products.AsNoTracking() on cart.ProductId equals product.Id
        where cart.UserId == userId
        select new CartItemResponse(cart.Id, cart.ProductId, cart.SkuId, product.Name, sku.SkuCode, sku.Specification,
            sku.Price, cart.Quantity, cart.Checked, sku.Stock - sku.LockedStock, sku.Price * cart.Quantity);
}
