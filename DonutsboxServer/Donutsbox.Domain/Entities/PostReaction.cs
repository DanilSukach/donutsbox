using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Сущность реакции на пост
/// </summary>
[Table("post_reaction")]
public class PostReaction
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Идентификатор поста
    /// </summary>
    [Column("post_id", TypeName = "uuid")]
    [Required]
    public required Guid ContentPostId { get; set; }
    public required ContentPost ContentPost { get; set; }
    /// <summary>
    /// Идентификатор юзера
    /// </summary>
    [Column("user_id", TypeName = "uuid")]
    [Required]
    public required Guid UserId { get; set; }
    /// <summary>
    /// Разновидность реакции (лайк, дизлайк и т.д.)
    /// </summary>
    [Column("reaction_type_id", TypeName = "int")]
    [Required]
    public required int ReactionTypeId { get; set; }
}
