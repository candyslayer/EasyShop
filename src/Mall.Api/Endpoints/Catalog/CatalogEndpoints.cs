using Mall.Api.Application.Catalog;
using Mall.Api.Endpoints.Auth;

namespace Mall.Api.Endpoints.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/categories", GetCategoriesAsync).WithTags("Catalog");
        endpoints.MapGet("/api/products", GetProductsAsync).WithTags("Catalog");
        endpoints.MapGet("/api/products/{id:long}", GetProductAsync).WithTags("Catalog");
        var favorites = endpoints.MapGroup("/api/favorites").WithTags("Favorites").RequireAuthorization();
        favorites.MapPost("/{productId:long}", AddFavoriteAsync);
        favorites.MapGet("", ListFavoritesAsync);
        favorites.MapDelete("/{productId:long}", RemoveFavoriteAsync);
        return endpoints;
    }

    private static Task<CategoryResponse[]> GetCategoriesAsync(CatalogUseCases useCases, CancellationToken cancellationToken) =>
        useCases.GetCategoriesAsync(cancellationToken);

    private static Task<ProductListResponse> GetProductsAsync(
        CatalogUseCases useCases,
        long? categoryId,
        long[]? categoryIds,
        string? keyword,
        string? brand,
        string? tag,
        string? attribute,
        decimal? minPrice,
        decimal? maxPrice,
        string? sort,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        useCases.GetProductsAsync(categoryId, categoryIds, keyword, brand, tag, attribute, minPrice, maxPrice, sort, page, pageSize, cancellationToken);

    private static async Task<IResult> GetProductAsync(long id, CatalogUseCases useCases, CancellationToken cancellationToken)
    {
        var product = await useCases.GetProductAsync(id, cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private static async Task<IResult> AddFavoriteAsync(HttpContext context, long productId, FavoriteUseCases useCases, CancellationToken ct)
    { if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized(); var result = await useCases.AddAsync(userId, productId, ct); return result.Value is null ? TypedResults.BadRequest(new ErrorResponse(result.Error!)) : TypedResults.Ok(result.Value); }
    private static async Task<IResult> ListFavoritesAsync(HttpContext context, FavoriteUseCases useCases, int page = 1, int pageSize = 20, CancellationToken ct = default)
    { if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized(); return TypedResults.Ok(await useCases.ListAsync(userId, page, pageSize, ct)); }
    private static async Task<IResult> RemoveFavoriteAsync(HttpContext context, long productId, FavoriteUseCases useCases, CancellationToken ct)
    { if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized(); return await useCases.RemoveAsync(userId, productId, ct) ? TypedResults.NoContent() : TypedResults.NotFound(); }
}
