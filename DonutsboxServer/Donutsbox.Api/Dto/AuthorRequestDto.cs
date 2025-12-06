namespace Donutsbox.Api.Dto;

public class AuthorRequestDto
{
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Название страницы автора
    /// </summary>
    public required string PageName { get; set; } = null!;

    /// <summary>
    /// Ссылка на аватарку автора
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Ссылка на аватарку автора
    /// </summary>
    public string? BannerUrl { get; set; }

    /// <summary>
    /// Описание страницы автора
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Количество подписчиков
    /// </summary>
    public required int SubscribersCount { get; set; }

    /// <summary>
    /// Подписки
    /// </summary>
    public required List<SubscriptionDto> Subscriptions { get; set; } = [];

    /// <summary>
    /// Находится ли автор в теневом бане
    /// </summary>
    public bool IsShadowBanned { get; set; } = false;
}
