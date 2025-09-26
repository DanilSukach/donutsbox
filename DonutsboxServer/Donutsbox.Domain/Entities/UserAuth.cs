using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Данные аунтентификации
/// </summary>
[Table("UsersAuth")]
public class UserAuth
{
    /// <summary>
    /// Id
    /// </summary>
    [Key]
    [Column("Id", TypeName = "uuid")]
    [Required]
    public required Guid Id { get; set; }
    /// <summary>
    /// Email для аунтентификации
    /// </summary>
    [Column("AuthEmail")]
    [Required]
    public required string AuthEmail { get; set; }
    /// <summary>
    /// пассворд
    /// </summary>
    [Column("Password")]
    [Required]
    public required string Password { get; set; }
    /// <summary>
    /// Дата последнего входа
    /// </summary>
    [Column("LastAuth")]
    public DateTime? LastAuth { get; set; }
    /// <summary>
    /// Refresh Token
    /// </summary>
    [Column("Refresh_token")]
    public string? RefreshToken { get; set; }
    /// <summary>
    /// Время истечения RefreshToken
    /// </summary>
    [Column("Refresh_token_expiry_time")]
    public DateTime? RefreshTokenExpiryTime { get; set; }
}

