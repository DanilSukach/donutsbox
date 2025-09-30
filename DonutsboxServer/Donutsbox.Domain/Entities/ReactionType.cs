using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Типы реакций на пост
/// </summary>
[Table("ReactionType")]
public class ReactionType
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("Id")]
    public required int Id { get; set; }
    /// <summary>
    /// Название типа реакции
    /// </summary>
    [Column("Name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
}
