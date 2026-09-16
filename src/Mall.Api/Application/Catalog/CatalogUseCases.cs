using Mall.Api.Infrastructure.Persistence;
using Mall.Api.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Catalog;

public sealed class CatalogUseCases(MallDbContext db, CatalogCache cache)
{
    public async Task<CategoryResponse[]> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetCategoriesAsync(cancellationToken);
        if (cached is not null) return cached;
        var result = await db.Categories.AsNoTracking()
            .Where(x => x.Enabled)
            .OrderBy(x => x.ParentId).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new CategoryResponse(x.Id, x.ParentId, x.Name, x.IconUrl, x.SortOrder))
            .ToArrayAsync(cancellationToken);
        await cache.SetCategoriesAsync(result, cancellationToken);
        return result;
    }

    public async Task<ProductListResponse> GetProductsAsync(long? categoryId, long[]? categoryIds, string? keyword, string? brand, string? tag, string? attribute, decimal? minPrice, decimal? maxPrice, string? sort, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Clamp(page, 1, 100_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var cacheKey = $"{categoryId}:{string.Join(',', categoryIds ?? [])}:{keyword}:{brand}:{tag}:{attribute}:{minPrice}:{maxPrice}:{sort}:{page}:{pageSize}";
        var cached = await cache.GetProductsAsync(cacheKey, cancellationToken);
        if (cached is not null) return cached;
        var query = db.Products.AsNoTracking().Where(x => x.IsOnSale);
        if (categoryId is not null) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (categoryIds is { Length: > 0 }) query = query.Where(x => categoryIds.Contains(x.CategoryId));
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{keyword.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(brand)) query = query.Where(x => x.Brand == brand);
        if (!string.IsNullOrWhiteSpace(tag)) query = query.Where(x => EF.Functions.ILike(x.Tags, $"%\"{tag.Trim()}\"%"));
        if (!string.IsNullOrWhiteSpace(attribute)) query = query.Where(x => EF.Functions.ILike(x.Attributes, $"%{attribute.Trim()}%"));
        if (minPrice is not null) query = query.Where(x => x.MaxPrice >= minPrice.Value);
        if (maxPrice is not null) query = query.Where(x => x.MinPrice <= maxPrice.Value);
        var total = await query.CountAsync(cancellationToken);
        var ordered = sort?.ToLowerInvariant() switch
        {
            "sales" => query.OrderByDescending(x => x.SalesCount).ThenByDescending(x => x.Id),
            "price_asc" => query.OrderBy(x => x.MinPrice).ThenByDescending(x => x.Id),
            "price_desc" => query.OrderByDescending(x => x.MaxPrice).ThenByDescending(x => x.Id),
            "rating" => query.OrderByDescending(x => x.RatingAverage).ThenByDescending(x => x.ReviewCount),
            _ => query.OrderByDescending(x => x.IsRecommended).ThenByDescending(x => x.SalesCount).ThenByDescending(x => x.RatingAverage).ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
        };
        var items = await ordered
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProductListItem(x.Id, x.CategoryId, x.Name, x.Subtitle, x.MinPrice, x.MaxPrice,
                x.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(), x.Brand, x.Tags, x.SalesCount, x.RatingAverage, x.ReviewCount, x.IsRecommended))
            .ToArrayAsync(cancellationToken);
        var result = new ProductListResponse(page, pageSize, total, items);
        await cache.SetProductsAsync(cacheKey, result, cancellationToken);
        return result;
    }

    public async Task<ProductDetailResponse?> GetProductAsync(long id, CancellationToken cancellationToken)
    {
        var cached = await cache.GetProductAsync(id, cancellationToken);
        if (cached is not null) return cached;
        var product = await db.Products.AsNoTracking()
            .Where(x => x.Id == id && x.IsOnSale)
            .Select(x => new ProductDetailResponse(
                x.Id, x.CategoryId, x.Name, x.Subtitle, x.Description, x.MinPrice, x.MaxPrice,
                x.Category == null ? null : x.Category.Name,
                x.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                    .Select(i => new ProductImageResponse(i.Id, i.ImageUrl, i.SortOrder, i.IsPrimary)).ToArray(),
                x.Skus.Where(s => s.Enabled).OrderBy(s => s.Price)
                    .Select(s => new ProductSkuResponse(s.Id, s.SkuCode, s.Specification, s.Price, s.MarketPrice, s.Stock - s.LockedStock, s.Enabled)).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
        if (product is not null) await cache.SetProductAsync(id, product, cancellationToken);
        return product;
    }
}
