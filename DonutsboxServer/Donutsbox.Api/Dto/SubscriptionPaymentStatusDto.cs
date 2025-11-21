namespace Donutsbox.Api.Dto;

public class SubscriptionPaymentStatusDto
{
    /// <summary>
    /// Идентификатор заявки на оплату в нашей системе.
    /// </summary>
    public required Guid PaymentRequestId { get; set; }

    /// <summary>
    /// Идентификатор платежа в YooKassa, если он получен.
    /// </summary>
    public string? PaymentId { get; set; }

    /// <summary>
    /// Текущий статус платежа (pending, waiting_for_capture, succeeded, canceled и т.д.).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Статус подписки после обработки платежа.
    /// </summary>
    public string? SubscriptionStatus { get; set; }

    /// <summary>
    /// Дата, до которой активна подписка (если подписка активна).
    /// </summary>
    public DateTime? SubscriptionEndDate { get; set; }

    /// <summary>
    /// Истекает ли ссылка на оплату и когда.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Ссылка на подтверждение оплаты (если платеж ещё не завершен).
    /// </summary>
    public string? ConfirmationUrl { get; set; }
}

