using Mall.Api.Application.Admin;
using Mall.Api.Domain.Orders;

namespace Mall.Api.Endpoints.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization("admin");
        group.MapGet("/users", (AdminUseCases useCases, CancellationToken ct) => useCases.UsersAsync(ct));
        group.MapGet("/users/{id:long}", GetUserAsync);
        group.MapPut("/users/{id:long}/status", UpdateUserStatusAsync);
        group.MapGet("/roles", (AdminUseCases useCases, CancellationToken ct) => useCases.RolesAsync(ct));
        group.MapPost("/roles", (SaveRoleRequest request, AdminUseCases useCases, CancellationToken ct) => useCases.SaveRoleAsync(request, ct));
        group.MapDelete("/roles/{id:long}", DeleteRoleAsync);
        group.MapPost("/categories", CreateCategoryAsync);
        group.MapPut("/categories/{id:long}", UpdateCategoryAsync);
        group.MapDelete("/categories/{id:long}", DeleteCategoryAsync);
        group.MapPut("/categories/{id:long}/status", UpdateCategoryStatusAsync);
        group.MapGet("/permissions", (AdminUseCases useCases, CancellationToken ct) => useCases.PermissionsAsync(ct));
        group.MapPost("/permissions", (SavePermissionRequest request, AdminUseCases useCases, CancellationToken ct) => useCases.SavePermissionAsync(request, ct));
        group.MapGet("/products", (AdminUseCases useCases, int? page, int? pageSize, string? keyword, long? categoryId, bool? onSale, CancellationToken ct) => useCases.ProductsAsync(Math.Max(page ?? 1, 1), Math.Max(pageSize ?? 20, 1), keyword, categoryId, onSale, ct));
        group.MapGet("/products/{id:long}", GetProductAsync);
        group.MapPost("/products", CreateProductAsync);
        group.MapPut("/products/{id:long}", UpdateProductAsync);
        group.MapDelete("/products/{id:long}", DeleteProductAsync);
        group.MapGet("/products/{id:long}/inventory", GetInventoryAsync);
        group.MapPost("/products/{id:long}/sale", SetProductSaleAsync);
        group.MapGet("/orders", (AdminUseCases useCases, int? page, int? pageSize, string? keyword, OrderStatus? status, CancellationToken ct) => useCases.OrdersAsync(Math.Max(page ?? 1, 1), Math.Max(pageSize ?? 20, 1), keyword, status, ct));
        group.MapGet("/orders/{id:long}", GetOrderAsync);
        group.MapPost("/orders/{id:long}/ship", ShipOrderAsync);
        return endpoints;
    }

    private static async Task<IResult> SetProductSaleAsync(long id, bool onSale, AdminUseCases useCases, CancellationToken ct) =>
        await useCases.SetProductSaleAsync(id, onSale, ct) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static async Task<IResult> GetProductAsync(long id, AdminUseCases useCases, CancellationToken ct) => (await useCases.ProductAsync(id, ct)) is { } value ? TypedResults.Ok(value) : TypedResults.NotFound();
    private static async Task<IResult> CreateCategoryAsync(SaveAdminCategoryRequest request, AdminUseCases useCases, CancellationToken ct) => Results.Created($"/api/admin/categories/{await useCases.CreateCategoryAsync(request, ct)}", null);
    private static async Task<IResult> UpdateCategoryAsync(long id, SaveAdminCategoryRequest request, AdminUseCases useCases, CancellationToken ct) => await useCases.UpdateCategoryAsync(id, request, ct) ? TypedResults.NoContent() : TypedResults.NotFound();
    private static async Task<IResult> DeleteCategoryAsync(long id, AdminUseCases useCases, CancellationToken ct) => (await useCases.DeleteCategoryAsync(id, ct)) switch { DeleteAdminCategoryResult.Deleted => TypedResults.NoContent(), DeleteAdminCategoryResult.NotFound => TypedResults.NotFound(), _ => TypedResults.Conflict(new { message = "分类下存在商品或子分类，无法删除。" }) };
    private static async Task<IResult> UpdateCategoryStatusAsync(long id, UpdateAdminCategoryStatusRequest request, AdminUseCases useCases, CancellationToken ct) => await useCases.UpdateCategoryStatusAsync(id, request, ct) ? TypedResults.NoContent() : TypedResults.NotFound();
    private static async Task<IResult> DeleteRoleAsync(long id, AdminUseCases useCases, CancellationToken ct) => (await useCases.DeleteRoleAsync(id, ct)) switch
    {
        DeleteAdminRoleResult.Deleted => TypedResults.NoContent(),
        DeleteAdminRoleResult.NotFound => TypedResults.NotFound(),
        _ => TypedResults.Conflict(new { message = "角色仍被用户使用，无法删除。" })
    };
    private static async Task<IResult> GetUserAsync(long id, AdminUseCases useCases, CancellationToken ct) => (await useCases.UserAsync(id, ct)) is { } value ? TypedResults.Ok(value) : TypedResults.NotFound();
    private static async Task<IResult> UpdateUserStatusAsync(long id, UpdateAdminUserStatusRequest request, AdminUseCases useCases, CancellationToken ct) => (await useCases.UpdateUserStatusAsync(id, request, ct)) is { } value ? TypedResults.Ok(value) : TypedResults.NotFound();
    private static async Task<IResult> CreateProductAsync(SaveAdminProductRequest request, AdminUseCases useCases, CancellationToken ct) => Results.Created($"/api/admin/products/{await useCases.CreateProductAsync(request, ct)}", null);
    private static async Task<IResult> UpdateProductAsync(long id, SaveAdminProductRequest request, AdminUseCases useCases, CancellationToken ct) => await useCases.UpdateProductAsync(id, request, ct) ? TypedResults.NoContent() : TypedResults.NotFound();
    private static async Task<IResult> DeleteProductAsync(long id, AdminUseCases useCases, CancellationToken ct) => await useCases.DeleteProductAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound();
    private static async Task<IResult> GetInventoryAsync(long id, AdminUseCases useCases, CancellationToken ct) => (await useCases.InventoryAsync(id, ct)) is { } value ? TypedResults.Ok(value) : TypedResults.NotFound();
    private static async Task<IResult> GetOrderAsync(long id, AdminUseCases useCases, CancellationToken ct) => (await useCases.OrderAsync(id, ct)) is { } value ? TypedResults.Ok(value) : TypedResults.NotFound();

    private static async Task<IResult> ShipOrderAsync(long id, ShipOrderRequest request, AdminUseCases useCases, CancellationToken ct) =>
        await useCases.ShipOrderAsync(id, request, ct) ? TypedResults.NoContent() : TypedResults.BadRequest();
}
