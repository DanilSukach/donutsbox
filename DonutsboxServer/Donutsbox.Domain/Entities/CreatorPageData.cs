using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Данные о странице автора
/// </summary>
[Table("creator_page_data")]
public class CreatorPageData
{
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Идентификатор автора(пользователя)
    /// </summary>
    [Column("guid", TypeName = "uuid")]
    [Required]
    public required Guid UserId { get; set; }
    public required User User { get; set; }
    /// <summary>
    /// Название страницы автора
    /// </summary>
    [Column("page_name")]
    [MaxLength(40)]
    [Required]
    public required string PageName { get; set; }
    /// <summary>
    /// Ссылка на аватарку автора
    /// </summary>
    [Column("avatar_url")]
    [Required]
    public required string AvatarURL { get; set; }
    /// <summary>
    /// Ссылка на баннер автора
    /// </summary>
    [Column("banner_url")]
    [Required]
    public required string BannerURL { get; set; }
    /// <summary>
    /// Описание страницы автора
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }
    /// <summary>
    /// Количество подписчиков
    /// </summary>
    [Column("subscribers_count")]
    [Required]
    public required int SubscribersCount { get; set; }
    public List<ContentPost> ContentPosts { get; set; } = [];
    public List<Subscription> Subscriptions { get; set; } = [];
}
