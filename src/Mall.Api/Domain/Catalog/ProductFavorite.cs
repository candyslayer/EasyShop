namespace Mall.Api.Domain.Catalog;

public sealed class ProductFavorite
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public DateTime CreatedAt { get; set; }
}
