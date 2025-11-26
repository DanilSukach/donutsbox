using System.Text.Json.Serialization;

namespace Donutsbox.Api.Services.Payments;

public record YooKassaCreatePaymentRequest(
    [property: JsonPropertyName("amount")] YooKassaAmount Amount,
    [property: JsonPropertyName("capture")] bool Capture,
    [property: JsonPropertyName("confirmation")] YooKassaConfirmation Confirmation,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("metadata")] IDictionary<string, string>? Metadata = null);

public record YooKassaAmount(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("currency")] string Currency);

public record YooKassaConfirmation(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("return_url")] string ReturnUrl);

public record YooKassaPaymentResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("paid")]
    public bool Paid { get; init; }

    [JsonPropertyName("amount")]
    public required YooKassaAmount Amount { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("confirmation")]
    public YooKassaConfirmationResponse? Confirmation { get; init; }

    [JsonPropertyName("metadata")]
    public IDictionary<string, string>? Metadata { get; init; }
}

public record YooKassaConfirmationResponse
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("confirmation_url")]
    public string? ConfirmationUrl { get; init; }
}

public record YooKassaWebhook
{
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("object")]
    public required YooKassaPaymentResponse Object { get; init; }
}

