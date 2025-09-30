using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Доп инфа о пользователе
/// </summary>
[Table("user_data")]
public class UserData
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Id непоссредственного пользователя
    /// </summary>
    [Column("user_id", TypeName = "uuid")]
    [Required]
    public required Guid UserId { get; set; }
    [Required]
    public required User User { get; set; }
    /// <summary>
    /// Ссылка на хранилище аватарки
    /// </summary>
    [Column("avatar_url")]
    [Required]
    public required string AvatarUrl { get; set; }
    /// <summary>
    /// Раздел "о себе" пользователя
    /// </summary>
    [Column("description")]
    [MaxLength(300)]
    public string? Description { get; set; }
    /// <summary>
    /// Email для уведомлений
    /// </summary>
    [Column("notification_email")]
    public string? NotificationEmail { get; set; }
    /// <summary>
    /// Номер телефона
    /// </summary>
    [Column("phone_number")]
    [MaxLength(11)]
    public string? PhoneNumber { get; set; }
    /// <summary>
    /// TBD
    /// </summary>
    [Column("payment_info")]
    public string? PaymentInfo { get; set; }
}

