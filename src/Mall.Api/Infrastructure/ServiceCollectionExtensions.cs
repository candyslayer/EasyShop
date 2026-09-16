using Mall.Api.Infrastructure.Persistence;
using Mall.Api.Application.Auth;
using Mall.Api.Application.Catalog;
using Mall.Api.Application.Cart;
using Mall.Api.Application.Orders;
using Mall.Api.Application.Payments;
using Mall.Api.Application.Admin;
using Mall.Api.Application.Promotions;
using Mall.Api.Infrastructure.Authentication;
using Mall.Api.Infrastructure.WeChat;
using Mall.Api.Infrastructure.Caching;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Mall.Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddOptions<RedisOptions>().Bind(configuration.GetSection(RedisOptions.SectionName));
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddOptions<WeChatOptions>().Bind(configuration.GetSection(WeChatOptions.SectionName));
        services.AddOptions<PaymentOptions>().Bind(configuration.GetSection(PaymentOptions.SectionName));
        var redis = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new();
        services.AddStackExchangeRedisCache(options => options.Configuration = redis.ConnectionString);
        services.AddDbContext<MallDbContext>((serviceProvider, options) =>
        {
            var database = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(database.ConnectionString, npgsql => npgsql.EnableRetryOnFailure(3));
        });
        services.AddHttpClient<IWeChatSessionClient, WeChatSessionClient>();
        services.AddSingleton<JwtTokenIssuer>();
        services.AddScoped<AuthUseCases>();
        services.AddScoped<UserUseCases>();
        services.AddScoped<CatalogUseCases>();
        services.AddScoped<CartUseCases>();
        services.AddScoped<OrderUseCases>();
        services.AddScoped<PaymentUseCases>();
        services.AddScoped<AdminUseCases>();
        services.AddScoped<PromotionUseCases>();
        services.AddSingleton<CatalogCache>();
        services.AddSingleton<IPaymentGateway, MockPaymentGateway>();
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();
        var key = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwt.SigningKey)
            ? "development-only-signing-key-change-me" : jwt.SigningKey);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30), RoleClaimType = "role"
            });
        services.AddAuthorization(options => options.AddPolicy("admin", policy => policy.RequireRole("admin")));
        return services;
    }
}
