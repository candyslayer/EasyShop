namespace Mall.Api.Application.Catalog;

public sealed record CategoryResponse(long Id, long? ParentId, string Name, string? IconUrl, int SortOrder);

public sealed record ProductListItem(
    long Id,
    long CategoryId,
    string Name,
    string? Subtitle,
    decimal MinPrice,
    decimal MaxPrice,
    string? PrimaryImageUrl,
    string? Brand,
    string Tags,
    int SalesCount,
    decimal RatingAverage,
    int ReviewCount,
    bool IsRecommended);

public sealed record ProductListResponse(int Page, int PageSize, int Total, ProductListItem[] Items);

public sealed record ProductDetailResponse(
    long Id,
    long CategoryId,
    string Name,
    string? Subtitle,
    string? Description,
    decimal MinPrice,
    decimal MaxPrice,
    string? CategoryName,
    ProductImageResponse[] Images,
    ProductSkuResponse[] Skus);

public sealed record ProductImageResponse(long Id, string ImageUrl, int SortOrder, bool IsPrimary);

public sealed record ProductSkuResponse(
    long Id,
    string SkuCode,
    string? Specification,
    decimal Price,
    decimal MarketPrice,
    int AvailableStock,
    bool Enabled);
