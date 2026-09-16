using Mall.Api.Application.Orders;
using Mall.Api.Domain.Orders;
using Mall.Api.Endpoints.Auth;

namespace Mall.Api.Endpoints.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();
        group.MapPost("", CreateAsync);
        group.MapPost("/preview", PreviewAsync);
        group.MapGet("", ListAsync);
        group.MapGet("/{id:long}", GetAsync);
        group.MapPost("/{id:long}/cancel", CancelAsync);
        group.MapPost("/{id:long}/finish", FinishAsync);
        group.MapPost("/after-sales", ApplyAfterSaleAsync);
        group.MapGet("/after-sales", ListAfterSalesAsync);
        group.MapGet("/{id:long}/logistics", LogisticsAsync);
        group.MapPost("/{id:long}/review", ReviewAsync);
        return endpoints;
    }
    private static async Task<IResult> PreviewAsync(HttpContext context, PreviewOrderRequest request, OrderUseCases u, CancellationToken ct)
    { if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized(); var result = await u.PreviewAsync(userId, request, ct); return result is null ? TypedResults.BadRequest(new ErrorResponse("订单预览信息无效。")) : TypedResults.Ok(result); }

    private static async Task<IResult> CreateAsync(HttpContext context, CreateOrderRequest request, OrderUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await useCases.CreateAsync(userId, request, cancellationToken);
        return result.Succeeded ? TypedResults.Ok(result.Order) : TypedResults.BadRequest(new ErrorResponse(result.Error!));
    }

    private static async Task<IResult> ListAsync(HttpContext context, OrderUseCases useCases, OrderStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        return TypedResults.Ok(await useCases.ListAsync(userId, status, page, pageSize, cancellationToken));
    }

    private static async Task<IResult> GetAsync(HttpContext context, long id, OrderUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await useCases.GetAsync(userId, id, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CancelAsync(HttpContext context, long id, OrderUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        return await useCases.CancelAsync(userId, id, cancellationToken) ? TypedResults.NoContent() : TypedResults.BadRequest(new ErrorResponse("订单不存在或当前状态不可取消。"));
    }

    private static async Task<IResult> FinishAsync(HttpContext context, long id, OrderUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        return await useCases.FinishAsync(userId, id, cancellationToken) ? TypedResults.NoContent() : TypedResults.BadRequest(new ErrorResponse("订单不存在或当前状态不可确认收货。"));
    }
    private static async Task<IResult> ApplyAfterSaleAsync(HttpContext c, AfterSaleRequest request, OrderExtraUseCases u, CancellationToken ct) { if (!c.User.TryGetUserId(out var id)) return TypedResults.Unauthorized(); var r = await u.ApplyAfterSaleAsync(id, request, ct); return r.Value is null ? TypedResults.BadRequest(new ErrorResponse(r.Error!)) : TypedResults.Ok(r.Value); }
    private static async Task<IResult> ListAfterSalesAsync(HttpContext c, OrderExtraUseCases u, CancellationToken ct) { if (!c.User.TryGetUserId(out var id)) return TypedResults.Unauthorized(); return TypedResults.Ok(await u.AfterSalesAsync(id, ct)); }
    private static async Task<IResult> LogisticsAsync(HttpContext c, long id, OrderExtraUseCases u, CancellationToken ct) { if (!c.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized(); var r = await u.LogisticsAsync(userId, id, ct); return r is null ? TypedResults.NotFound() : TypedResults.Ok(r); }
    private static async Task<IResult> ReviewAsync(HttpContext c, long id, ReviewRequest request, OrderExtraUseCases u, CancellationToken ct) { if (!c.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized(); var r = await u.ReviewAsync(userId, id, request, ct); return r.Value is null ? TypedResults.BadRequest(new ErrorResponse(r.Error!)) : TypedResults.Ok(r.Value); }
}
