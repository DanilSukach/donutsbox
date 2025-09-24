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
}