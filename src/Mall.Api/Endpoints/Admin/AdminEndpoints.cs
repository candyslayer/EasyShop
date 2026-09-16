using Mall.Api.Application.Admin;

namespace Mall.Api.Endpoints.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization("admin");
        group.MapGet("/users", (AdminUseCases useCases, CancellationToken ct) => useCases.UsersAsync(ct));
        group.MapGet("/roles", (AdminUseCases useCases, CancellationToken ct) => useCases.RolesAsync(ct));
        group.MapPost("/roles", (SaveRoleRequest request, AdminUseCases useCases, CancellationToken ct) => useCases.SaveRoleAsync(request, ct));
        group.MapGet("/permissions", (AdminUseCases useCases, CancellationToken ct) => useCases.PermissionsAsync(ct));
        group.MapPost("/permissions", (SavePermissionRequest request, AdminUseCases useCases, CancellationToken ct) => useCases.SavePermissionAsync(request, ct));
        group.MapGet("/products", (AdminUseCases useCases, CancellationToken ct) => useCases.ProductsAsync(ct));
        group.MapPost("/products/{id:long}/sale", SetProductSaleAsync);
        group.MapGet("/orders", (AdminUseCases useCases, CancellationToken ct) => useCases.OrdersAsync(ct));
        group.MapPost("/orders/{id:long}/ship", ShipOrderAsync);
        return endpoints;
    }

    private static async Task<IResult> SetProductSaleAsync(long id, bool onSale, AdminUseCases useCases, CancellationToken ct) =>
        await useCases.SetProductSaleAsync(id, onSale, ct) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static async Task<IResult> ShipOrderAsync(long id, ShipOrderRequest request, AdminUseCases useCases, CancellationToken ct) =>
        await useCases.ShipOrderAsync(id, request, ct) ? TypedResults.NoContent() : TypedResults.BadRequest();
}
