using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Доп инфа о пользователе
/// </summary>
[Table("UsersData")]
public class UserData
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("GUID", TypeName = "uuid")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Id непоссредственного пользователя
    /// </summary>
    [Column("UserId", TypeName = "uuid")]
    [Required]
    public required Guid UserId { get; set; }
    /// <summary>
    /// Ссылка на хранилище аватарки
    /// </summary>
    [Column("AvatarUrl")]
    [Required]
    public required string AvatarUrl { get; set; }
    /// <summary>
    /// Раздел "о себе" пользователя
    /// </summary>
    [Column("Description")]
    [MaxLength(300)]
    public string? Description { get; set; }
    /// <summary>
    /// Email для уведомлений
    /// </summary>
    [Column("NotificationEmail")]
    public string? NotificationEmail { get; set; }
    /// <summary>
    /// Номер телефона
    /// </summary>
    [Column("PhoneNumber")]
    [MaxLength(11)]
    public string? PhoneNumber { get; set; }
    /// <summary>
    /// TBD
    /// </summary>
    [Column("PaymentInfo")]
    public string? PaymentInfo { get; set; }
}

