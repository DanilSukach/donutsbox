namespace Donutsbox.Api.Dto;

public class UserSubscriptionCreateDto
{
    /// <summary>
    /// Идентификатор подписки (тип подписки)
    /// </summary>
    public required Guid SubscriptionId { get; set; }
}