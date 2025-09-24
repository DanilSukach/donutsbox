namespace Donutsbox.Api.Dto;

public class CreatorPageDataDto
{
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    public required Guid PageId { get; set; }
    /// <summary>
    /// Название страницы автора
    /// </summary>
    public required string PageName { get; set; }
    /// <summary>
    /// Ссылка на аватарку автора
    /// </summary>
    public required string AvatarURL { get; set; }
    /// <summary>
    /// Ссылка на баннер автора
    /// </summary>
    public required string BannerURL { get; set; }
    /// <summary>
    /// Описание страницы автора
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Количество подписчиков
    /// </summary>
    public required int SubscribersCount { get; set; }
}