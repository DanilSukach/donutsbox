using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Тип пользователя
/// </summary>
[Table("UserTypes")]
public class UserType
{
    /// <summary>
    /// Id типа пользователя
    /// </summary>
    [Key]
    [Column("Id")]
    public required int Id { get; set; }
    /// <summary>
    /// Имя типа
    /// </summary>
    [Column("Name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
}

