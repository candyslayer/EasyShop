namespace Mall.Api.Domain.Payments;

public enum PaymentStatus
{
    Created,
    Paid,
    Failed,
    Refunded
}

public sealed class PaymentRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string PaymentNo { get; set; } = string.Empty;
    public string Channel { get; set; } = "WECHAT";
    public PaymentStatus Status { get; set; } = PaymentStatus.Created;
    public decimal Amount { get; set; }
    public string? ExternalTransactionNo { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
