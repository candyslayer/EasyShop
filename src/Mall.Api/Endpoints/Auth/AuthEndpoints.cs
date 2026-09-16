using System.Security.Claims;
using Mall.Api.Application.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Mall.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Auth");
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/wechat-login", WeChatLoginAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, AuthUseCases useCases, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length is < 3 or > 100 ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length is < 6 or > 128)
            return TypedResults.BadRequest(new ErrorResponse("用户名或密码格式无效。"));
        var result = await useCases.RegisterAsync(request, cancellationToken);
        return result is null ? TypedResults.Conflict(new ErrorResponse("用户名已存在。")) : TypedResults.Ok(result);
    }

    private static async Task<IResult> LoginAsync(PasswordLoginRequest request, AuthUseCases useCases, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return TypedResults.BadRequest(new ErrorResponse("用户名和密码不能为空。"));
        var result = await useCases.LoginAsync(request, cancellationToken);
        return result is null ? TypedResults.Unauthorized() : TypedResults.Ok(result);
    }

    private static async Task<IResult> WeChatLoginAsync(WeChatLoginRequest request, AuthUseCases useCases, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(request.Code)
            ? TypedResults.BadRequest(new ErrorResponse("微信登录 code 不能为空。"))
            : TypedResults.Ok(await useCases.WeChatLoginAsync(request, cancellationToken));
}

public sealed record ErrorResponse(string Message);

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal user, out long userId) =>
        long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out userId);
}
