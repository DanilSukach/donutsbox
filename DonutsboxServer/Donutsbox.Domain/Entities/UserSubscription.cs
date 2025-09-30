using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Сущность подписки пользователя на страницу автора
/// </summary>
[Table("user_subscription")]
public class UserSubscription
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Идентификатор пользователя, который подписан
    /// </summary>
    [Column("user_id", TypeName = "uuid")]
    [Required]
    public required Guid UserId { get; set; }
    /// <summary>
    /// Идентификатор подписки (тип подписки)
    /// </summary>
    [Column("subscription_id", TypeName = "uuid")]
    [Required]
    public required Guid SubscriptionId { get; set; }
    /// <summary>
    /// Дата начала подписки
    /// </summary>
    [Column("begin_date")]
    [Required]
    public required DateTime BeginDate { get; set; }
    /// <summary>
    /// Дата конца подписки
    /// </summary>
    [Column("end_date")]
    [Required]
    public required DateTime EndDate { get; set; }

}
