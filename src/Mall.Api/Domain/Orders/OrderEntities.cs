namespace Mall.Api.Domain.Orders;

public enum OrderStatus
{
    WaitPay,
    Paid,
    Shipped,
    Finished,
    Cancelled
}

public sealed class Order
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.WaitPay;
    public decimal GoodsAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public string Consignee { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SkuCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public Order? Order { get; set; }
}
