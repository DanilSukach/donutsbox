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
    /// Id непоссредственного пользователя
    /// </summary>
    [Column("GUID")]
    [Required]
    public required string GUID { get; set; }
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

