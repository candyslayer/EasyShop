using Mall.Api.Application.Auth;
using Mall.Api.Endpoints.Auth;

namespace Mall.Api.Endpoints.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users").WithTags("Users").RequireAuthorization();
        group.MapGet("/me", GetMeAsync);
        group.MapGet("/addresses", GetAddressesAsync);
        group.MapPost("/addresses", AddAddressAsync);
        group.MapPut("/addresses/{id:long}", UpdateAddressAsync);
        group.MapDelete("/addresses/{id:long}", DeleteAddressAsync);
        return endpoints;
    }

    private static async Task<IResult> GetMeAsync(HttpContext httpContext, UserUseCases useCases, CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var user = await useCases.GetAsync(userId, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(new UserProfileResponse(user.Id, user.Username, user.Mobile, user.Nickname, user.AvatarUrl));
    }

    private static async Task<IResult> GetAddressesAsync(HttpContext httpContext, UserUseCases useCases, CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var addresses = await useCases.GetAddressesAsync(userId, cancellationToken);
        return TypedResults.Ok(addresses.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> AddAddressAsync(HttpContext httpContext, AddressRequest request, UserUseCases useCases, CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        if (!IsValid(request)) return TypedResults.BadRequest(new ErrorResponse("收货地址格式无效。"));
        return TypedResults.Ok(ToResponse(await useCases.AddAddressAsync(userId, request, cancellationToken)));
    }

    private static async Task<IResult> UpdateAddressAsync(HttpContext httpContext, long id, AddressRequest request, UserUseCases useCases, CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        if (!IsValid(request)) return TypedResults.BadRequest(new ErrorResponse("收货地址格式无效。"));
        var address = await useCases.UpdateAddressAsync(userId, id, request, cancellationToken);
        return address is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(address));
    }

    private static async Task<IResult> DeleteAddressAsync(HttpContext httpContext, long id, UserUseCases useCases, CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        return await useCases.DeleteAddressAsync(userId, id, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static AddressResponse ToResponse(Domain.Users.UserAddress address) => new(address.Id, address.Consignee, address.Mobile, address.Province, address.City, address.District, address.Detail, address.IsDefault);
    private static bool IsValid(AddressRequest request) =>
        !string.IsNullOrWhiteSpace(request.Consignee) && request.Consignee.Length <= 100 &&
        !string.IsNullOrWhiteSpace(request.Mobile) && request.Mobile.Length <= 32 &&
        !string.IsNullOrWhiteSpace(request.Province) && request.Province.Length <= 50 &&
        !string.IsNullOrWhiteSpace(request.City) && request.City.Length <= 50 &&
        !string.IsNullOrWhiteSpace(request.District) && request.District.Length <= 50 &&
        !string.IsNullOrWhiteSpace(request.Detail) && request.Detail.Length <= 255;
}

public sealed record AddressResponse(long Id, string Consignee, string Mobile, string Province, string City, string District, string Detail, bool IsDefault);
