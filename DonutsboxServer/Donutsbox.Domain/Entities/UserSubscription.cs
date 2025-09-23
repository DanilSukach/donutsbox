using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

[Table("UserSubscription")]
/// <summary>
/// Сущность подписки пользователя на страницу автора
/// </summary>
public class UserSubscription
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("Id")]
    public required string Id { get; set; }
    /// <summary>
    /// Идентификатор пользователя, который подписан
    /// </summary>
    [Column("UserId")]
    [Required]
    public required string UserId { get; set; }
    /// <summary>
    /// Идентификатор подписки (тип подписки)
    /// </summary>
    [Column("SubscriptionId")]
    [Required]
    public required string SubscriptionId { get; set; }
    /// <summary>
    /// Дата начала подписки
    /// </summary>
    [Column("BeginDate")]
    [Required]
    public required DateTime BeginDate { get; set; }
    /// <summary>
    /// Дата конца подписки
    /// </summary>
    [Column("EndDate")]
    [Required]
    public required DateTime EndDate { get; set; }

}
