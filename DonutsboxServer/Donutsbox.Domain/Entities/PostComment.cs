using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Сущность комментария к посту
/// </summary>
[Table("PostComment")]
public class PostComment
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
    /// Текст комментария
    /// </summary>
    [Column("Text", TypeName = "text")]
    [Required]
    public required string Text { get; set; }
    /// <summary>
    /// Дата создания комментария
    /// </summary>
    [Column("CreatedAt", TypeName = "timestamptz")]
    [Required]
    public required DateTimeOffset CreatedAt { get; set; }
}
