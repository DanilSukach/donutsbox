namespace Donutsbox.Api.Dto;

public class SubscriptionPaymentResponseDto
{
    /// <summary>
    /// Идентификатор заявки на оплату в нашей системе.
    /// </summary>
    public required Guid PaymentRequestId { get; set; }

    /// <summary>
    /// Идентификатор платежа в YooKassa.
    /// </summary>
    public required string PaymentId { get; set; }

    /// <summary>
    /// Ссылка, на которую необходимо перенаправить пользователя для оплаты.
    /// </summary>
    public required string ConfirmationUrl { get; set; }

    /// <summary>
    /// Текущий статус платежа (pending/succeeded/canceled/...).
    /// </summary>
    public required string Status { get; set; }
}

