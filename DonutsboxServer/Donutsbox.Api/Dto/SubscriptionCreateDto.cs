namespace Donutsbox.Api.Dto;

public class SubscriptionCreateDto
{
    /// <summary>
    /// Цена подписки
    /// </summary>
    public required string Price { get; set; }
    /// <summary>
    /// Название подписки
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Описание подписки
    /// </summary>
    public required string Description { get; set; }
    /// <summary>
    /// Ссылка на картинку подписки
    /// </summary>
    public string? PictureURL { get; set; }
}