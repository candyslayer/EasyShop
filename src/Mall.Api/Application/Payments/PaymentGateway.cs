namespace Mall.Api.Application.Payments;

public interface IPaymentGateway
{
    ValueTask<string> CreatePrepayAsync(string paymentNo, decimal amount, CancellationToken cancellationToken);
}

public sealed class MockPaymentGateway : IPaymentGateway
{
    public ValueTask<string> CreatePrepayAsync(string paymentNo, decimal amount, CancellationToken cancellationToken) =>
        ValueTask.FromResult($"mock-prepay-{paymentNo}");
}
