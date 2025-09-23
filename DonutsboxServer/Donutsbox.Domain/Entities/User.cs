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
    public required string GUID { get; set; }
    /// <summary>
    /// Имя пользователя
    /// </summary>
    [Column("Name")]
    [Required]
    public required string Name { get; set; }
    /// <summary>
    /// Тип пользователя
    /// </summary>
    [Column("TypeID")]
    [Required]
    public required string TypeID { get; set; }
    /// <summary>
    /// Сущность для авторизации
    /// </summary>
    [Column("AuthID")]
    [Required]
    public required string AuthID { get; set; }
}
