using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Сущность комментария к посту
/// </summary>
[Table("post_comment")]
public class PostComment
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
    public required Guid PostId { get; set; }
    /// <summary>
    /// Идентификатор юзера
    /// </summary>
    [Column("user_id", TypeName = "uuid")]
    [Required]
    public required Guid UserId { get; set; }
    /// <summary>
    /// Текст комментария
    /// </summary>
    [Column("text", TypeName = "text")]
    [Required]
    public required string Text { get; set; }
    /// <summary>
    /// Дата создания комментария
    /// </summary>
    [Column("created_at", TypeName = "timestamptz")]
    [Required]
    public required DateTimeOffset CreatedAt { get; set; }
}
