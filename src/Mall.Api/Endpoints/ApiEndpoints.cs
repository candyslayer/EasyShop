using Mall.Api.Common;
using Mall.Api.Endpoints.Auth;
using Mall.Api.Endpoints.Users;
using Mall.Api.Endpoints.Catalog;
using Mall.Api.Endpoints.Cart;
using Mall.Api.Endpoints.Orders;
using Mall.Api.Endpoints.Payments;
using Mall.Api.Endpoints.Admin;
using Mall.Api.Endpoints.Promotions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Mall.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup(ApiConstants.ApiPrefix);
        api.MapGet("/", GetApiInfo).WithTags("System");
        api.MapGet("/version", GetVersion).WithTags("System");
        endpoints.MapAuthEndpoints();
        endpoints.MapUserEndpoints();
        endpoints.MapCatalogEndpoints();
        endpoints.MapCartEndpoints();
        endpoints.MapOrderEndpoints();
        endpoints.MapPaymentEndpoints();
        endpoints.MapAdminEndpoints();
        endpoints.MapPromotionEndpoints();
        return endpoints;
    }

    private static Ok<ApiInfo> GetApiInfo() => TypedResults.Ok(new ApiInfo("Mall.Api", "微信商城 ASP.NET Core 10 API", "第一阶段基础宿主已就绪"));
    private static Ok<ApiVersion> GetVersion() => TypedResults.Ok(new ApiVersion("0.1.0"));
}

public sealed record ApiInfo(string Name, string Description, string Status);
public sealed record ApiVersion(string Version);
