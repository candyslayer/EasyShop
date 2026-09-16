using Mall.Api.Domain.Orders;

namespace Mall.Api.Application.Orders;

public sealed record CreateOrderRequest(long AddressId, long[]? CartItemIds = null, long? FlashSaleActivityId = null, long? GroupBuyActivityId = null, long? GroupBuyTeamId = null);
public sealed record CancelOrderRequest(string? Reason = null);

public sealed record OrderItemResponse(long Id, long ProductId, long SkuId, string ProductName, string SkuCode, decimal UnitPrice, int Quantity, decimal TotalAmount);

public sealed record OrderResponse(
    long Id,
    string OrderNo,
    OrderStatus Status,
    decimal GoodsAmount,
    decimal FreightAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string Consignee,
    string Mobile,
    string Address,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? ShippedAt,
    DateTime? FinishedAt,
    OrderItemResponse[] Items);

public sealed record OrderListResponse(int Page, int PageSize, int Total, OrderResponse[] Items);
public sealed record OrderCreateResult(OrderResponse? Order, string? Error)
{
    public bool Succeeded => Order is not null;
}
