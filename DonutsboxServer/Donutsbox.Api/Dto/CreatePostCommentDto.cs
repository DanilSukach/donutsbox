using System.ComponentModel.DataAnnotations;

namespace Donutsbox.Api.Dto;

/// <summary>
/// DTO для создания комментария
/// </summary>
public class CreatePostCommentDto
{
    /// <summary>
    /// Идентификатор поста
    /// </summary>
    [Required]
    public required Guid PostId { get; set; }

    /// <summary>
    /// Текст комментария
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public required string Text { get; set; }
}
