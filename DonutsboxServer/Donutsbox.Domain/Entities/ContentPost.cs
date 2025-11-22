using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Данные о посте
/// </summary>
[Table("content_post")]
public class ContentPost
{
    /// <summary>
    /// Идентификатор поста
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    [Column("page_id", TypeName = "uuid")]
    [Required]
    public required Guid CreatorPageDataId { get; set; }
    public required CreatorPageData CreatorPageData { get; set; }
    /// <summary>
    /// Заголовок поста
    /// </summary>
    [Column("title")]
    [Required]
    public string? Title { get; set; }
    /// <summary>
    /// Текст поста
    /// </summary>
    [Column("text")]
    [Required]
    public string? Text { get; set; }
    /// <summary>
    /// Статус поста
    /// </summary>
    [Column("is_published")]
    [Required]
    public bool IsPublished { get; set; } = false;
    /// <summary>
    /// Дата создания поста
    /// </summary>
    [Column("created_at", TypeName = "timestamptz")]
    [Required]
    public DateTimeOffset? CreatedAt { get; set; }
    /// <summary>
    /// Кол-во лайков
    /// </summary>
    [Column("likes_count")]
    [Required]
    public required int LikesCount { get; set; }
    /// <summary>
    /// Кол-во дизлайков
    /// </summary>
    [Column("dislikes_count")]
    [Required]
    public required int DislikesCount { get; set; }
    /// <summary>
    /// Кол-во комментариев
    /// </summary>
    [Column("comments_count")]
    [Required]
    public required int CommentsCount { get; set; }
    /// <summary>
    /// Ссылки на аудио
    /// </summary>
    [Column("audio_urls")]
    [Required]
    public List<string> AudioURLs { get; set; } = [];
    /// <summary>
    /// Ссылки на видео
    /// </summary>
    [Column("video_urls")]
    [Required]
    public List<Video> Videos { get; set; } = [];
    /// <summary>
    /// Ссылки на картинки  
    /// </summary>
    [Column("picture_urls")]
    [Required]
    public List<string> Images { get; set; } = [];
    public string AudienceType { get; set; } = "Public";
    public List<PostReaction> PostReactions { get; set; } = [];
    public List<PostComment> PostComments { get; set; } = [];
    public List<Subscription> Subscriptions { get; set; } = [];
}
