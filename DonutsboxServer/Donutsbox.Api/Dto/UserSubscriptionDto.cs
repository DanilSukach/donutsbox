namespace Donutsbox.Api.Dto;

public class UserSubscriptionDto
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public required Guid Id { get; set; }
    /// <summary>
    /// Идентификатор пользователя, который подписан
    /// </summary>
    public required Guid UserId { get; set; }
    /// <summary>
    /// Идентификатор подписки (тип подписки)
    /// </summary>
    public required Guid SubscriptionId { get; set; }
    /// <summary>
    /// Дата начала подписки
    /// </summary>
    public required DateTime BeginDate { get; set; }
    /// <summary>
    /// Дата конца подписки
    /// </summary>
    public required DateTime EndDate { get; set; }
    /// <summary>
    /// Статус подписки
    /// </summary>
    public required string Status { get; set; }
    /// <summary>
    /// Идентификатор платежа, активировавшего подписку
    /// </summary>
    public string? PaymentId { get; set; }
    /// <summary>
    /// Дата создания записи подписки
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Дата последнего обновления записи подписки
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }
}