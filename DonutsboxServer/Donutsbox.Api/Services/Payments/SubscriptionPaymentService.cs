using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Donutsbox.Api.Services.Payments;

public class SubscriptionPaymentService : ISubscriptionPaymentService
{
    private readonly DonutsboxDbContext dbContext;
    private readonly IYooKassaClient yooKassaClient;
    private readonly YooKassaOptions options;
    private readonly ILogger<SubscriptionPaymentService> logger;

    public SubscriptionPaymentService(
        DonutsboxDbContext dbContext,
        IYooKassaClient yooKassaClient,
        IOptions<YooKassaOptions> options,
        ILogger<SubscriptionPaymentService> logger)
    {
        this.dbContext = dbContext;
        this.yooKassaClient = yooKassaClient;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<SubscriptionPaymentResponseDto> CreateSubscriptionPaymentAsync(SubscriptionPaymentRequestDto request, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var subscription = await dbContext.Subscriptions
            .Include(s => s.SubscriptionPeriod)
            .Include(s => s.CreatorPageData)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found");

        var userEntity = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var amount = ParsePrice(subscription.Price);
        if (amount <= 0)
        {
            throw new InvalidOperationException("Invalid subscription price");
        }

        var id = Guid.NewGuid();
        var idempotenceKey = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        var paymentRecord = new SubscriptionPayment
        {
            Id = id,
            UserId = userId,
            SubscriptionId = subscription.Id,
            Subscription = subscription,
            User = userEntity,
            Amount = amount,
            Currency = "RUB",
            Status = "pending",
            Description = $"Подписка «{subscription.Name}»",
            IdempotenceKey = idempotenceKey,
            CreatedAt = now
        };

        dbContext.SubscriptionPayments.Add(paymentRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        var metadata = new Dictionary<string, string>
        {
            ["subscription_payment_id"] = paymentRecord.Id.ToString(),
            ["subscription_id"] = subscription.Id.ToString(),
            ["user_id"] = userId.ToString()
        };

        var confirmationUrl = BuildReturnUrl(request.ReturnUrl, paymentRecord.Id);
        var paymentRequest = new YooKassaCreatePaymentRequest(
            new YooKassaAmount(amount.ToString("F2", CultureInfo.InvariantCulture), paymentRecord.Currency),
            options.CapturePayment,
            new YooKassaConfirmation("redirect", confirmationUrl),
            paymentRecord.Description!,
            metadata);

        var yooKassaPayment = await yooKassaClient.CreatePaymentAsync(paymentRequest, idempotenceKey, cancellationToken);

        paymentRecord.PaymentId = yooKassaPayment.Id;
        paymentRecord.Status = yooKassaPayment.Status;
        paymentRecord.ConfirmationUrl = yooKassaPayment.Confirmation?.ConfirmationUrl ?? confirmationUrl;
        paymentRecord.ExpiresAt = yooKassaPayment.ExpiresAt;
        paymentRecord.MetadataJson = SerializeMetadata(yooKassaPayment.Metadata);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubscriptionPaymentResponseDto
        {
            PaymentRequestId = paymentRecord.Id,
            PaymentId = paymentRecord.PaymentId!,
            ConfirmationUrl = paymentRecord.ConfirmationUrl!,
            Status = paymentRecord.Status
        };
    }

    public async Task HandleWebhookAsync(YooKassaWebhook webhook, IHeaderDictionary headers, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received YooKassa webhook for payment {PaymentId} with event {Event}", webhook.Object.Id, webhook.Event);

        if (!ValidateWebhookAuthorization(headers))
        {
            logger.LogWarning("YooKassa webhook rejected due to invalid authorization header");
            throw new UnauthorizedAccessException("Invalid webhook authorization");
        }

        logger.LogInformation("Processing YooKassa webhook: {Event} for payment {PaymentId}", webhook.Event, webhook.Object.Id);

        switch (webhook.Event)
        {
            case "payment.succeeded":
                await HandlePaymentSucceededAsync(webhook.Object, cancellationToken);
                break;
            case "payment.waiting_for_capture":
            case "payment.canceled":
            case "payment.expired":
                await UpdatePaymentStatusAsync(webhook.Object, cancellationToken);
                break;
            default:
                logger.LogInformation("Unhandled YooKassa event type: {Event}", webhook.Event);
                break;
        }
    }

    public async Task<SubscriptionPaymentStatusDto?> GetPaymentStatusAsync(Guid paymentRequestId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var payment = await dbContext.SubscriptionPayments
            .Include(p => p.UserSubscription)
            .FirstOrDefaultAsync(p => p.Id == paymentRequestId && p.UserId == userId, cancellationToken);

        if (payment == null)
        {
            return null;
        }

        if (payment.PaymentId != null &&
            (string.Equals(payment.Status, "pending", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(payment.Status, "waiting_for_capture", StringComparison.OrdinalIgnoreCase)))
        {
            var remote = await yooKassaClient.GetPaymentAsync(payment.PaymentId, cancellationToken);
            if (remote != null && !string.Equals(remote.Status, payment.Status, StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = remote.Status;
                payment.ConfirmationUrl = remote.Confirmation?.ConfirmationUrl ?? payment.ConfirmationUrl;
                payment.ExpiresAt = remote.ExpiresAt ?? payment.ExpiresAt;
                payment.MetadataJson = SerializeMetadata(remote.Metadata);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return new SubscriptionPaymentStatusDto
        {
            PaymentRequestId = payment.Id,
            PaymentId = payment.PaymentId,
            Status = payment.Status,
            ConfirmationUrl = payment.ConfirmationUrl,
            ExpiresAt = payment.ExpiresAt,
            SubscriptionStatus = payment.UserSubscription?.Status,
            SubscriptionEndDate = payment.UserSubscription?.EndDate
        };
    }

    private async Task HandlePaymentSucceededAsync(YooKassaPaymentResponse paymentResponse, CancellationToken cancellationToken)
    {
        var paymentRecord = await FindPaymentRecordAsync(paymentResponse, cancellationToken);
        if (paymentRecord == null)
        {
            logger.LogWarning("Payment record not found for YooKassa payment {PaymentId}", paymentResponse.Id);
            return;
        }

        var subscription = await dbContext.Subscriptions
            .Include(s => s.SubscriptionPeriod)
            .Include(s => s.CreatorPageData)
            .FirstOrDefaultAsync(s => s.Id == paymentRecord.SubscriptionId, cancellationToken);
        if (subscription == null)
        {
            logger.LogWarning("Subscription {SubscriptionId} not found while handling payment {PaymentId}", paymentRecord.SubscriptionId, paymentResponse.Id);
            return;
        }

        var now = DateTime.UtcNow;
        var months = subscription.SubscriptionPeriod.Months;

        var userEntity = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == paymentRecord.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found while handling payment");

        var userSubscription = await dbContext.UsersSubscriptions
            .FirstOrDefaultAsync(us => us.UserId == paymentRecord.UserId && us.SubscriptionId == paymentRecord.SubscriptionId, cancellationToken);

        var hadActive = userSubscription != null && string.Equals(userSubscription.Status, "active", StringComparison.OrdinalIgnoreCase) && userSubscription.EndDate >= now;

        if (userSubscription == null)
        {
            userSubscription = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = paymentRecord.UserId,
                User = userEntity,
                SubscriptionId = paymentRecord.SubscriptionId,
                Subscription = subscription,
                BeginDate = now,
                EndDate = now.AddMonths(months),
                Status = "active",
                PaymentId = paymentResponse.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            dbContext.UsersSubscriptions.Add(userSubscription);
        }
        else
        {
            if (userSubscription.EndDate < now)
            {
                userSubscription.BeginDate = now;
                userSubscription.EndDate = now.AddMonths(months);
            }
            else
            {
                userSubscription.EndDate = userSubscription.EndDate.AddMonths(months);
            }

            userSubscription.Status = "active";
            userSubscription.PaymentId = paymentResponse.Id;
            userSubscription.UpdatedAt = DateTimeOffset.UtcNow;
        }

        paymentRecord.Status = paymentResponse.Status;
        paymentRecord.PaymentId = paymentResponse.Id;
        paymentRecord.ExpiresAt = paymentResponse.ExpiresAt ?? paymentRecord.ExpiresAt;
        paymentRecord.ConfirmationUrl = paymentResponse.Confirmation?.ConfirmationUrl ?? paymentRecord.ConfirmationUrl;
        paymentRecord.MetadataJson = SerializeMetadata(paymentResponse.Metadata);
        paymentRecord.UserSubscriptionId = userSubscription.Id;

        var becameActive = !hadActive && userSubscription.EndDate >= now && string.Equals(userSubscription.Status, "active", StringComparison.OrdinalIgnoreCase);
        if (becameActive)
        {
            var creatorPage = await dbContext.CreatorsPageData.FirstOrDefaultAsync(c => c.Id == subscription.CreatorPageDataId, cancellationToken);
            if (creatorPage != null)
            {
                creatorPage.SubscribersCount += 1;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Activated subscription {SubscriptionId} for user {UserId} via payment {PaymentId}", paymentRecord.SubscriptionId, paymentRecord.UserId, paymentResponse.Id);
    }

    private async Task UpdatePaymentStatusAsync(YooKassaPaymentResponse paymentResponse, CancellationToken cancellationToken)
    {
        var paymentRecord = await FindPaymentRecordAsync(paymentResponse, cancellationToken);
        if (paymentRecord == null)
        {
            logger.LogWarning("Payment record not found for YooKassa payment {PaymentId}", paymentResponse.Id);
            return;
        }

        paymentRecord.Status = paymentResponse.Status;
        paymentRecord.PaymentId = paymentResponse.Id;
        paymentRecord.ExpiresAt = paymentResponse.ExpiresAt ?? paymentRecord.ExpiresAt;
        paymentRecord.ConfirmationUrl = paymentResponse.Confirmation?.ConfirmationUrl ?? paymentRecord.ConfirmationUrl;
        paymentRecord.MetadataJson = SerializeMetadata(paymentResponse.Metadata);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubscriptionPayment?> FindPaymentRecordAsync(YooKassaPaymentResponse paymentResponse, CancellationToken cancellationToken)
    {
        Guid? paymentRecordId = null;
        if (paymentResponse.Metadata != null &&
            paymentResponse.Metadata.TryGetValue("subscription_payment_id", out var metadataValue) &&
            Guid.TryParse(metadataValue, out var parsedId))
        {
            paymentRecordId = parsedId;
        }

        var query = dbContext.SubscriptionPayments
            .Include(p => p.Subscription)
                .ThenInclude(s => s.SubscriptionPeriod)
            .Include(p => p.Subscription)
                .ThenInclude(s => s.CreatorPageData)
            .Include(p => p.UserSubscription);

        if (paymentRecordId.HasValue)
        {
            return await query.FirstOrDefaultAsync(p => p.Id == paymentRecordId.Value, cancellationToken);
        }

        return await query.FirstOrDefaultAsync(p => p.PaymentId == paymentResponse.Id, cancellationToken);
    }

    private static string? SerializeMetadata(IDictionary<string, string>? metadata)
        => metadata == null ? null : JsonSerializer.Serialize(metadata);

    private bool ValidateWebhookAuthorization(IHeaderDictionary headers)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            return true;
        }

        var headerValue = headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(headerValue) && headerValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            if (IsValidBasicAuth(headerValue))
            {
                return true;
            }
        }

        if (headers.TryGetValue("signature", out var signatureValues))
        {
            var signature = signatureValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(signature))
            {
                logger.LogInformation("YooKassa webhook accepted via signature header");
                return true;
            }
        }

        return false;
    }

    private bool IsValidBasicAuth(string headerValue)
    {
        var provided = headerValue["Basic ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(provided))
        {
            return false;
        }

        string decoded;
        try
        {
            var bytes = Convert.FromBase64String(provided);
            decoded = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex < 0)
        {
            return false;
        }

        var login = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];

        if (!string.Equals(password, options.WebhookSecret, StringComparison.Ordinal))
        {
            logger.LogWarning("Rejected YooKassa webhook: invalid secret");
            return false;
        }

        if (string.IsNullOrEmpty(login) || string.Equals(login, options.ShopId, StringComparison.Ordinal))
        {
            return true;
        }

        logger.LogWarning("Rejected YooKassa webhook: unexpected login value '{Login}'", login);
        return false;
    }

    private string BuildReturnUrl(string? requestReturnUrl, Guid paymentRequestId)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(requestReturnUrl) ? requestReturnUrl : options.DefaultReturnUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Return URL is not configured");
        }

        return QueryHelpers.AddQueryString(baseUrl, "paymentRequestId", paymentRequestId.ToString());
    }

    private static decimal ParsePrice(string priceRaw)
    {
        var normalized = priceRaw.Replace(',', '.').Trim();
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found");
        return Guid.Parse(claim.Value);
    }
}

