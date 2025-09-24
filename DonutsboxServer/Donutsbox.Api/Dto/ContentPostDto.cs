namespace Donutsbox.Api.Dto;

public class ContentPostDto
{
    /// <summary>
    /// Идентификатор поста
    /// </summary>
    public required Guid PostId { get; set; }
    /// <summary>
    /// Идентификатор страницы автора
    /// </summary>
    public required Guid PageId { get; set; }
    /// <summary>
    /// Заголовок поста
    /// </summary>
    public string? Title { get; set; }
    /// <summary>
    /// Текст поста
    /// </summary>
    public string? Text { get; set; }
    /// <summary>
    /// Дата создания поста
    /// </summary>
    public required DateTime CreatedAt { get; set; }
    /// <summary>
    /// Кол-во дизлайков
    /// </summary>
    public required int DislikesCount { get; set; }
    /// <summary>
    /// Ссылки на аудио
    /// </summary>
    public required List<string> AudioURLs { get; set; }
    /// <summary>
    /// Ссылки на видео
    /// </summary>
    public required List<string> VideoURLs { get; set; }
    /// <summary>
    /// Ссылки на картинки  
    /// </summary>
    public required List<string> PictureURLs { get; set; }
}