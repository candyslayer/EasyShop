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
        group.MapGet("/{id:long}/status", QueryAsync).RequireAuthorization();
        group.MapPost("/refunds", RefundAsync).RequireAuthorization();
        group.MapPost("/wechat/callback", CallbackAsync);
        group.MapPost("/wechat/notify", WeChatNotifyAsync);
        group.MapPost("/reconciliation/{date}", ReconcileAsync).RequireAuthorization("admin");
        return endpoints;
    }
    private static async Task<IResult> QueryAsync(HttpContext c, long id, PaymentUseCases u, CancellationToken ct)
    { if (!c.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized(); var result = await u.QueryAsync(userId, id, ct); return result is null ? TypedResults.NotFound() : TypedResults.Ok(result); }
    private static async Task<IResult> RefundAsync(HttpContext c, RefundRequest request, PaymentUseCases u, CancellationToken ct)
    { if (!c.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized(); var result = await u.RefundAsync(userId, request, ct); return result.Value is null ? TypedResults.BadRequest(new ErrorResponse(result.Error!)) : TypedResults.Ok(result.Value); }
    private static async Task<IResult> ReconcileAsync(DateOnly date, PaymentUseCases u, CancellationToken ct) => TypedResults.Ok(await u.ReconcileAsync(date, ct));
    private static async Task<IResult> WeChatNotifyAsync(HttpRequest request, PaymentUseCases u, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body); var body = await reader.ReadToEndAsync(ct);
        var ok = await u.HandleWechatCallbackAsync(body, request.Headers["Wechatpay-Timestamp"]!, request.Headers["Wechatpay-Nonce"]!, request.Headers["Wechatpay-Signature"]!, ct);
        return ok ? TypedResults.Ok(new { code = "SUCCESS", message = "成功" }) : TypedResults.BadRequest(new { code = "FAIL", message = "验签或处理失败" });
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
