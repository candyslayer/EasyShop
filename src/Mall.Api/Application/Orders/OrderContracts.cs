using Mall.Api.Domain.Orders;

namespace Mall.Api.Application.Orders;

public sealed record CreateOrderRequest(long AddressId, long[]? CartItemIds = null, long? FlashSaleActivityId = null, long? GroupBuyActivityId = null, long? GroupBuyTeamId = null, string? CouponCode = null, bool UsePoints = false, long? ProductId = null, long? SkuId = null, int Quantity = 1, string? Remark = null);
public sealed record PreviewOrderRequest(long AddressId, long[]? CartItemIds = null, long? ProductId = null, long? SkuId = null, int Quantity = 1, string? CouponCode = null, bool UsePoints = false);
public sealed record OrderPreviewResponse(OrderItemResponse[] Items, decimal GoodsAmount, decimal FreightAmount, decimal DiscountAmount, decimal PayableAmount, int UsedPoints, decimal PointsDiscount, string? CouponCode);
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
    string Remark,
    DateTime? PaymentExpiredAt,
    string? ShippingCompany,
    string? TrackingNo,
    OrderItemResponse[] Items);

public sealed record OrderListResponse(int Page, int PageSize, int Total, OrderResponse[] Items);
public sealed record OrderCreateResult(OrderResponse? Order, string? Error)
{
    public bool Succeeded => Order is not null;
}
