using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Данные о странице автора
/// </summary>
[Table("CreatorPageData")]
public class CreatorPageData
{
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    [Key]
    [Column("PageId", TypeName = "uuid")]
    public required Guid PageId { get; set; }
    /// <summary>
    /// Идентификатор автора(пользователя)
    /// </summary>
    [Column("GUID", TypeName = "uuid")]
    [Required]
    public required Guid GUID { get; set; }
    /// <summary>
    /// Название страницы автора
    /// </summary>
    [Column("PageName")]
    [MaxLength(40)]
    [Required]
    public required string PageName { get; set; }
    /// <summary>
    /// Ссылка на аватарку автора
    /// </summary>
    [Column("AvatarURL")]
    [Required]
    public required string AvatarURL { get; set; }
    /// <summary>
    /// Ссылка на баннер автора
    /// </summary>
    [Column("BannerURL")]
    [Required]
    public required string BannerURL { get; set; }
    /// <summary>
    /// Описание страницы автора
    /// </summary>
    [Column("Description")]
    public string? Description { get; set; }
    /// <summary>
    /// Количество подписчиков
    /// </summary>
    [Column("SubscribersCount")]
    [Required]
    public required int SubscribersCount { get; set; }
}
