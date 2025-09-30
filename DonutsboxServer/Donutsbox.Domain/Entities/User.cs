using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Обычный пользователь
/// </summary>
[Table("user")]
public class User
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid GUID { get; set; }
    /// <summary>
    /// Имя пользователя
    /// </summary>
    [Column("name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
    /// <summary>
    /// Тип пользователя
    /// </summary>
    [Column("type_id")]
    [Required]
    public required UserType Type { get; set; }
    /// <summary>
    /// Сущность для авторизации
    /// </summary>
    [Column("auth_id", TypeName = "uuid")]
    [Required]
    public required Guid AuthId { get; set; }
    /// <summary>
    /// Сущность для авторизации
    /// </summary>
    public required UserAuth UserAuth { get; set; }
}
