using Donutsbox.Api.Dto;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Donutsbox.Api.Services.Payments;

public interface ISubscriptionPaymentService
{
    Task<SubscriptionPaymentResponseDto> CreateSubscriptionPaymentAsync(SubscriptionPaymentRequestDto request, ClaimsPrincipal user, CancellationToken cancellationToken);

    Task HandleWebhookAsync(YooKassaWebhook webhook, IHeaderDictionary headers, CancellationToken cancellationToken);

    Task<SubscriptionPaymentStatusDto?> GetPaymentStatusAsync(Guid paymentRequestId, ClaimsPrincipal user, CancellationToken cancellationToken);
}

