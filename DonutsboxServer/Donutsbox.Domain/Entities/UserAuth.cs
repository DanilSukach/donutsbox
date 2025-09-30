using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Данные аунтентификации
/// </summary>
[Table("user_auth")]
public class UserAuth
{
    /// <summary>
    /// Id
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    [Required]
    public required Guid Id { get; set; }
    /// <summary>
    /// Пользователь
    /// </summary>
    [Column("user_id", TypeName = "uuid")]
    [Required]
    public required User User { get; set; }
    /// <summary>
    /// Email для аунтентификации
    /// </summary>
    [Column("auth_email")]
    [Required]
    public required string AuthEmail { get; set; }
    /// <summary>
    /// пассворд
    /// </summary>
    [Column("password")]
    [Required]
    public required string Password { get; set; }
    /// <summary>
    /// Дата последнего входа
    /// </summary>
    [Column("last_auth")]
    public DateTime? LastAuth { get; set; }
    /// <summary>
    /// Refresh Token
    /// </summary>
    [Column("refresh_token")]
    public string? RefreshToken { get; set; }
    /// <summary>
    /// Время истечения RefreshToken
    /// </summary>
    [Column("refresh_token_expiry_time")]
    public DateTime? RefreshTokenExpiryTime { get; set; }
}

