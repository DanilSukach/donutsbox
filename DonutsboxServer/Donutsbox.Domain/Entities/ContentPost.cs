using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Данные о посте
/// </summary>
[Table("ContentPost")]
public class ContentPost
{
    /// <summary>
    /// Идентификатор поста
    /// </summary>
    [Key]
    [Column("PostId")]
    public required Guid PostId { get; set; }
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    [Column("PageId")]
    [Required]
    public required Guid PageId { get; set; }
    /// <summary>
    /// Заголовок поста
    /// </summary>
    [Column("Title")]
    [Required]
    public string? Title { get; set; }
    /// <summary>
    /// Текст поста
    /// </summary>
    [Column("Text")]
    [Required]
    public string? Text { get; set; }
    /// <summary>
    /// Дата создания поста
    /// </summary>
    [Column("CreatedAt")]
    [Required]
    public required DateTime CreatedAt { get; set; }
    /// <summary>
    /// Кол-во дизлайков
    /// </summary>
    [Column("DislikesCount")]
    [Required]
    public required int DislikesCount { get; set; }
    /// <summary>
    /// Ссылки на аудио
    /// </summary>
    [Column("AudioURLs")]
    [Required]
    public required List<string> AudioURLs { get; set; }
    /// <summary>
    /// Ссылки на видео
    /// </summary>
    [Column("VideoURLs")]
    [Required]
    public required List<string> VideoURLs { get; set; }
    /// <summary>
    /// Ссылки на картинки  
    /// </summary>
    [Column("PictureURLs")]
    [Required]
    public required List<string> PictureURLs { get; set; }
}
