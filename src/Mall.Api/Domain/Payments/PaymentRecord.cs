namespace Mall.Api.Domain.Payments;

public enum PaymentStatus
{
    Created,
    Closed,
    Paid,
    Failed,
    Refunding,
    Refunded,
    Unknown
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
    public int QueryAttempts { get; set; }
    public DateTime? LastQueriedAt { get; set; }
}

public sealed class PaymentRefund
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public long OrderId { get; set; }
    public string RefundNo { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = "PROCESSING";
    public string? ExternalRefundNo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PaymentReconciliation
{
    public long Id { get; set; }
    public DateOnly TradeDate { get; set; }
    public string Channel { get; set; } = "WECHAT";
    public string Status { get; set; } = "PENDING";
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
