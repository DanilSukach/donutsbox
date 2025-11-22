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
    public required User User { get; set; }
    /// <summary>
    /// Идентификатор подписки (тип подписки)
    /// </summary>
    [Column("subscription_id", TypeName = "uuid")]
    [Required]
    public required Guid SubscriptionId { get; set; }
    public required Subscription Subscription { get; set; }
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
    /// <summary>
    /// Статус подписки (pending, active, expired, cancelled)
    /// </summary>
    [Column("status")]
    [MaxLength(32)]
    [Required]
    public string Status { get; set; } = "active";
    /// <summary>
    /// Идентификатор платежа, активировавшего подписку
    /// </summary>
    [Column("payment_id")]
    [MaxLength(128)]
    public string? PaymentId { get; set; }
    /// <summary>
    /// Дата создания записи
    /// </summary>
    [Column("created_at", TypeName = "timestamptz")]
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Дата обновления записи
    /// </summary>
    [Column("updated_at", TypeName = "timestamptz")]
    [Required]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
