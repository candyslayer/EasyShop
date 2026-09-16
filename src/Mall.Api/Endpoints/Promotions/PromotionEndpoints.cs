using Mall.Api.Application.Promotions;
using Mall.Api.Endpoints.Auth;

namespace Mall.Api.Endpoints.Promotions;

public static class PromotionEndpoints
{
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/promotions/group-buy", (PromotionUseCases u, CancellationToken ct) => u.GroupBuyActivitiesAsync(ct)).WithTags("Promotions");
        endpoints.MapGet("/api/promotions/group-buy/{activityId:long}/teams", (long activityId, PromotionUseCases u, CancellationToken ct) => u.TeamsAsync(activityId, ct)).WithTags("Promotions");
        var group = endpoints.MapGroup("/api/promotions").WithTags("Promotions").RequireAuthorization();
        group.MapPost("/group-buy/teams", CreateTeamAsync);
        group.MapPost("/group-buy/teams/{teamId:long}/join", JoinTeamAsync);
        group.MapGet("/flash-sale", (PromotionUseCases u, CancellationToken ct) => u.FlashSalesAsync(ct)).AllowAnonymous();
        group.MapPost("/flash-sale/reservations", ReserveFlashSaleAsync);
        group.MapPost("/distribution/activate", ActivateDistributorAsync);
        group.MapPost("/distribution/bind", BindDistributorAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateTeamAsync(HttpContext c, CreateGroupBuyTeamRequest request, PromotionUseCases u, CancellationToken ct)
    {
        if (!c.User.TryGetUserId(out var id)) return TypedResults.Unauthorized();
        var result = await u.CreateTeamAsync(id, request, ct); return result.Value is null ? TypedResults.BadRequest(new ErrorResponse(result.Error!)) : TypedResults.Ok(result.Value);
    }
    private static async Task<IResult> JoinTeamAsync(HttpContext c, long teamId, JoinGroupBuyTeamRequest request, PromotionUseCases u, CancellationToken ct)
    {
        if (!c.User.TryGetUserId(out var id)) return TypedResults.Unauthorized();
        var result = await u.JoinTeamAsync(id, teamId, request, ct); return result.Value is null ? TypedResults.BadRequest(new ErrorResponse(result.Error!)) : TypedResults.Ok(result.Value);
    }
    private static async Task<IResult> ReserveFlashSaleAsync(HttpContext c, FlashSaleReservationRequest request, PromotionUseCases u, CancellationToken ct)
    {
        if (!c.User.TryGetUserId(out var id)) return TypedResults.Unauthorized();
        var error = await u.ReserveFlashSaleAsync(id, request, ct); return error is null ? TypedResults.NoContent() : TypedResults.BadRequest(new ErrorResponse(error));
    }
    private static async Task<IResult> ActivateDistributorAsync(HttpContext c, PromotionUseCases u, CancellationToken ct)
    {
        if (!c.User.TryGetUserId(out var id)) return TypedResults.Unauthorized(); return TypedResults.Ok(await u.ActivateDistributorAsync(id, ct));
    }
    private static async Task<IResult> BindDistributorAsync(HttpContext c, BindDistributorRequest request, PromotionUseCases u, CancellationToken ct)
    {
        if (!c.User.TryGetUserId(out var id)) return TypedResults.Unauthorized(); var error = await u.BindDistributorAsync(id, request, ct); return error is null ? TypedResults.NoContent() : TypedResults.BadRequest(new ErrorResponse(error));
    }
}
