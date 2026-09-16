using Mall.Api.Application.Payments;
using Mall.Api.Endpoints.Auth;

namespace Mall.Api.Endpoints.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/payments").WithTags("Payments");
        group.MapPost("", CreateAsync).RequireAuthorization();
        group.MapGet("/{id:long}", GetAsync).RequireAuthorization();
        group.MapPost("/wechat/callback", CallbackAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(HttpContext context, CreatePaymentRequest request, PaymentUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await useCases.CreateAsync(userId, request, cancellationToken);
        return result.Succeeded ? TypedResults.Ok(result.Payment) : TypedResults.BadRequest(new ErrorResponse(result.Error!));
    }

    private static async Task<IResult> GetAsync(HttpContext context, long id, PaymentUseCases useCases, CancellationToken cancellationToken)
    {
        if (!context.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await useCases.GetAsync(userId, id, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CallbackAsync(PaymentCallbackRequest request, PaymentUseCases useCases, CancellationToken cancellationToken) =>
        await useCases.HandleCallbackAsync(request, cancellationToken) ? TypedResults.Ok(new PaymentCallbackResponse(true)) : TypedResults.BadRequest(new ErrorResponse("支付回调校验失败。"));
}

public sealed record PaymentCallbackResponse(bool Success);
