using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Обычный пользователь
/// </summary>
[Table("Users")]
public class User
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("GUID")]
    public required Guid GUID { get; set; }
    /// <summary>
    /// Имя пользователя
    /// </summary>
    [Column("Name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
    /// <summary>
    /// Тип пользователя
    /// </summary>
    [Column("TypeId")]
    [Required]
    public required int TypeId { get; set; }
    /// <summary>
    /// Сущность для авторизации
    /// </summary>
    [Column("AuthId")]
    [Required]
    public required string AuthId { get; set; }
}
