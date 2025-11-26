namespace Donutsbox.Api.Dto;

public class SubscriptionPaymentRequestDto
{
    /// <summary>
    /// Идентификатор подписки (тарифа), который покупает пользователь.
    /// </summary>
    public required Guid SubscriptionId { get; set; }

    /// <summary>
    /// URL, на который YooKassa вернёт пользователя после оплаты.
    /// Если не указан, будет использовано значение из конфигурации.
    /// </summary>
    public string? ReturnUrl { get; set; }
}

