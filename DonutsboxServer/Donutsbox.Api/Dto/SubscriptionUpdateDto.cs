namespace Donutsbox.Api.Dto;

public class SubscriptionUpdateDto
{
    /// <summary>
    /// Название подписки
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Описание подписки
    /// </summary>
    public required string Description { get; set; }
    /// <summary>
    /// Цена подписки (месячная)
    /// </summary>
    public required string Price { get; set; }
}

