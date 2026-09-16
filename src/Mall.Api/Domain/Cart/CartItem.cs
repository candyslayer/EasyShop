namespace Mall.Api.Domain.Cart;

public sealed class CartItem
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public int Quantity { get; set; }
    public bool Checked { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
