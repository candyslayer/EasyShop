using Mall.Api.Application.Catalog;

namespace Mall.Api.Endpoints.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/categories", GetCategoriesAsync).WithTags("Catalog");
        endpoints.MapGet("/api/products", GetProductsAsync).WithTags("Catalog");
        endpoints.MapGet("/api/products/{id:long}", GetProductAsync).WithTags("Catalog");
        return endpoints;
    }

    private static Task<CategoryResponse[]> GetCategoriesAsync(CatalogUseCases useCases, CancellationToken cancellationToken) =>
        useCases.GetCategoriesAsync(cancellationToken);

    private static Task<ProductListResponse> GetProductsAsync(
        CatalogUseCases useCases,
        long? categoryId,
        string? keyword,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        useCases.GetProductsAsync(categoryId, keyword, page, pageSize, cancellationToken);

    private static async Task<IResult> GetProductAsync(long id, CatalogUseCases useCases, CancellationToken cancellationToken)
    {
        var product = await useCases.GetProductAsync(id, cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }
}
