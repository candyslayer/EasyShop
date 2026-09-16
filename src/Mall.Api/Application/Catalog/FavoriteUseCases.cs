using Mall.Api.Infrastructure.Persistence;
using Mall.Api.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Catalog;

public sealed class FavoriteUseCases(MallDbContext db)
{
    public async Task<(FavoriteResponse? Value, string? Error)> AddAsync(long userId, long productId, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId && x.IsOnSale, ct);
        if (product is null) return (null, "商品不存在或已下架。");
        var favorite = await db.ProductFavorites.SingleOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, ct);
        if (favorite is null) { favorite = new ProductFavorite { UserId = userId, ProductId = productId, CreatedAt = DateTime.UtcNow }; db.ProductFavorites.Add(favorite); await db.SaveChangesAsync(ct); }
        return (new FavoriteResponse(favorite.Id, product.Id, product.Name, product.MinPrice, product.MaxPrice, favorite.CreatedAt), null);
    }

    public async Task<FavoriteListResponse> ListAsync(long userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 100_000); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = from f in db.ProductFavorites.AsNoTracking() join p in db.Products.AsNoTracking() on f.ProductId equals p.Id where f.UserId == userId select new FavoriteResponse(f.Id, p.Id, p.Name, p.MinPrice, p.MaxPrice, f.CreatedAt);
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct); return new(page, pageSize, total, items);
    }

    public async Task<bool> RemoveAsync(long userId, long productId, CancellationToken ct) => await db.ProductFavorites.Where(x => x.UserId == userId && x.ProductId == productId).ExecuteDeleteAsync(ct) > 0;
}

public sealed record FavoriteResponse(long Id, long ProductId, string ProductName, decimal MinPrice, decimal MaxPrice, DateTime CreatedAt);
public sealed record FavoriteListResponse(int Page, int PageSize, int Total, FavoriteResponse[] Items);
