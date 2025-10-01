using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Данные о подписке
/// </summary>
[Table("subscription")]
public class Subscription
{
    /// <summary>
    /// Идентификатор подписки
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    [Column("page_id", TypeName = "uuid")]
    public required Guid CreatorPageDataId { get; set; }
    public required CreatorPageData CreatorPageData { get; set; }
    /// <summary>
    /// Цена подписки
    /// </summary>
    [Column("price")]
    public required string Price { get; set; }
    /// <summary>
    /// Название подписки
    /// </summary>
    [Column("name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
    /// <summary>
    /// Описание подписки
    /// </summary>
    [Column("description")]
    [Required]
    public required string Description { get; set; }
    /// <summary>
    /// Ссылка на картинку подписки
    /// </summary>
    [Column("picture_url")]
    [Required]
    public required string PictureURL { get; set; }
    public List<UserSubscription> UserSubscriptions { get; set; } = [];
    /// <summary>
    /// Длительность подписки (месяц, год и т.д.)
    /// </summary>
    [Column("subscription_period_id", TypeName = "int")]
    [Required]
    public required int SubscriptionPeriodId { get; set; }
    public required SubscriptionPeriod SubscriptionPeriod { get; set; }
}
