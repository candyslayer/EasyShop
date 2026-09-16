using Mall.Api.Application.Cart;
using Mall.Api.Endpoints.Auth;

namespace Mall.Api.Endpoints.Cart;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/cart").WithTags("Cart").RequireAuthorization();
        group.MapGet("", GetAsync);
        group.MapPost("/items", AddAsync);
        group.MapPut("/items/{id:long}", UpdateAsync);
        group.MapDelete("/items/{id:long}", DeleteAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(HttpContext context, CartUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        return TypedResults.Ok(await useCases.GetAsync(userId, cancellationToken));
    }

    private static async Task<IResult> AddAsync(HttpContext context, AddCartItemRequest request, CartUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var item = await useCases.AddAsync(userId, request, cancellationToken);
        return item is null ? TypedResults.BadRequest(new ErrorResponse("SKU 不存在、已下架或库存不足。")) : TypedResults.Ok(item);
    }

    private static async Task<IResult> UpdateAsync(HttpContext context, long id, UpdateCartItemRequest request, CartUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var item = await useCases.UpdateAsync(userId, id, request, cancellationToken);
        return item is null ? TypedResults.BadRequest(new ErrorResponse("购物车项不存在或库存不足。")) : TypedResults.Ok(item);
    }

    private static async Task<IResult> DeleteAsync(HttpContext context, long id, CartUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        return await useCases.DeleteAsync(userId, id, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
