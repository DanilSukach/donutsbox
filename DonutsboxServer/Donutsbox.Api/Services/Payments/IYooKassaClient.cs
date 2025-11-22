namespace Donutsbox.Api.Services.Payments;

public interface IYooKassaClient
{
    Task<YooKassaPaymentResponse> CreatePaymentAsync(YooKassaCreatePaymentRequest request, string idempotenceKey, CancellationToken cancellationToken);

    Task<YooKassaPaymentResponse?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken);
}

