using Mall.Api.Domain.Payments;

namespace Mall.Api.Application.Payments;

public sealed record CreatePaymentRequest(long OrderId);
public sealed record PaymentCallbackRequest(string PaymentNo, bool Success, string? ExternalTransactionNo, string Signature);
public sealed record PaymentResponse(long Id, long OrderId, string PaymentNo, string Channel, PaymentStatus Status, decimal Amount, string? PrepayId, DateTime CreatedAt, DateTime? PaidAt);
public sealed record PaymentResult(PaymentResponse? Payment, string? Error)
{
    public bool Succeeded => Payment is not null;
}
