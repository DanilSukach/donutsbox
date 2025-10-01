using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Время подписки
/// </summary>
[Table("subscription_period")]
public class SubscriptionPeriod
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("id", TypeName = "int")]
    public required int Id { get; set; }
    /// <summary>
    /// Кол-во месяцев подписки
    /// </summary>
    [Column("months")]
    [Required]
    public required int Months { get; set; }
}
