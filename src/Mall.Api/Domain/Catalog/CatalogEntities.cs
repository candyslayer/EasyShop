namespace Mall.Api.Domain.Catalog;

public sealed class Category
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int SortOrder { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public List<Product> Products { get; set; } = [];
}

public sealed class Product
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public bool IsOnSale { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Category? Category { get; set; }
    public List<ProductSku> Skus { get; set; } = [];
    public List<ProductImage> Images { get; set; } = [];
}

public sealed class ProductSku
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public decimal Price { get; set; }
    public decimal MarketPrice { get; set; }
    public int Stock { get; set; }
    public int LockedStock { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Product? Product { get; set; }
}

public sealed class ProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public Product? Product { get; set; }
}
