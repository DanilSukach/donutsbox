using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Сущность подписки пользователя на страницу автора
/// </summary>
[Table("UserSubscription")]
public class UserSubscription
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("Id")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Идентификатор пользователя, который подписан
    /// </summary>
    [Column("UserId")]
    [Required]
    public required Guid UserId { get; set; }
    /// <summary>
    /// Идентификатор подписки (тип подписки)
    /// </summary>
    [Column("SubscriptionId")]
    [Required]
    public required Guid SubscriptionId { get; set; }
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
