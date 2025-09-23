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
    [Column("Id")]
    [Required]
    public required string Id { get; set; }
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
    [Required]
    public required DateTime LastAuth { get; set;}
}

