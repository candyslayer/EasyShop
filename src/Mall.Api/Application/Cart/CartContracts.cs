namespace Mall.Api.Application.Cart;

public sealed record AddCartItemRequest(long SkuId, int Quantity = 1, bool Checked = true);
public sealed record UpdateCartItemRequest(int Quantity, bool? Checked = null);

public sealed record CartItemResponse(
    long Id,
    long ProductId,
    long SkuId,
    string ProductName,
    string SkuCode,
    string? Specification,
    decimal UnitPrice,
    int Quantity,
    bool Checked,
    int AvailableStock,
    decimal TotalAmount);

public sealed record CartResponse(CartItemResponse[] Items, int CheckedCount, decimal CheckedAmount);
