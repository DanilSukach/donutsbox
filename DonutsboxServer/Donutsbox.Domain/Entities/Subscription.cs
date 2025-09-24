using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Данные о подписке
/// </summary>
[Table("Subscriptions")]
public class Subscription
{
    /// <summary>
    /// Идентификатор подписки
    /// </summary>
    [Key]
    [Column("SubscriptionId", TypeName = "uuid")]
    public required Guid SubscriptionId { get; set; }
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    [Column("PageId", TypeName = "uuid")]
    public required Guid PageId { get; set; }
    /// <summary>
    /// Цена подписки
    /// </summary>
    [Column("Price")]
    public required string Price { get; set; }
    /// <summary>
    /// Название подписки
    /// </summary>
    [Column("Name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
    /// <summary>
    /// Описание подписки
    /// </summary>
    [Column("Description")]
    [Required]
    public required string Description { get; set; }
    /// <summary>
    /// Ссылка на картинку подписки
    /// </summary>
    [Column("PictureURL")]
    [Required]
    public required string PictureURL { get; set; }
}
