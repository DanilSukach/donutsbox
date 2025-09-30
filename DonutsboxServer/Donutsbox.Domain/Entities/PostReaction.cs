using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Сущность реакции на пост
/// </summary>
[Table("PostReaction")]
public class PostReaction
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("GUID", TypeName = "uuid")]
    public required Guid GUID { get; set; }
    /// <summary>
    /// Идентификатор поста
    /// </summary>
    [Column("PostId", TypeName = "uuid")]
    [Required]
    public required Guid PostId { get; set; }
    /// <summary>
    /// Идентификатор юзера
    /// </summary>
    [Column("UserId", TypeName = "uuid")]
    [Required]
    public required Guid UserId { get; set; }
    /// <summary>
    /// Разновидность реакции (лайк, дизлайк и т.д.)
    /// </summary>
    [Column("ReactionTypeId", TypeName = "int")]
    [Required]
    public required int ReactionTypeId { get; set; }
}
