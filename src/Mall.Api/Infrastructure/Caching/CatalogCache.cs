using System.Text.Json;
using Mall.Api.Application.Catalog;
using Mall.Api.Common;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Mall.Api.Infrastructure.Caching;

public sealed class CatalogCache(IDistributedCache cache, ILogger<CatalogCache> logger)
{
    private static readonly DistributedCacheEntryOptions Options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) };

    public Task<CategoryResponse[]?> GetCategoriesAsync(CancellationToken ct) => GetAsync("catalog:categories", AppJsonSerializerContext.Default.CategoryResponseArray, ct);
    public Task SetCategoriesAsync(CategoryResponse[] value, CancellationToken ct) => SetAsync("catalog:categories", value, AppJsonSerializerContext.Default.CategoryResponseArray, ct);
    public Task InvalidateCategoriesAsync(CancellationToken ct) => cache.RemoveAsync("catalog:categories", ct);
    public Task<ProductListResponse?> GetProductsAsync(string key, CancellationToken ct) => GetAsync($"catalog:products:{key}", AppJsonSerializerContext.Default.ProductListResponse, ct);
    public Task SetProductsAsync(string key, ProductListResponse value, CancellationToken ct) => SetAsync($"catalog:products:{key}", value, AppJsonSerializerContext.Default.ProductListResponse, ct);
    public Task<ProductDetailResponse?> GetProductAsync(long id, CancellationToken ct) => GetAsync($"catalog:product:{id}", AppJsonSerializerContext.Default.ProductDetailResponse, ct);
    public Task SetProductAsync(long id, ProductDetailResponse value, CancellationToken ct) => SetAsync($"catalog:product:{id}", value, AppJsonSerializerContext.Default.ProductDetailResponse, ct);

    private async Task<T?> GetAsync<T>(string key, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        try { var bytes = await cache.GetAsync(key, ct); return bytes is null ? default : JsonSerializer.Deserialize(bytes, typeInfo); }
        catch (Exception exception) when (exception is RedisException or InvalidOperationException)
        { logger.LogWarning(exception, "Redis read failed for {CacheKey}", key); return default; }
    }

    private async Task SetAsync<T>(string key, T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        try { await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value, typeInfo), Options, ct); }
        catch (Exception exception) when (exception is RedisException or InvalidOperationException)
        { logger.LogWarning(exception, "Redis write failed for {CacheKey}", key); }
    }
}
