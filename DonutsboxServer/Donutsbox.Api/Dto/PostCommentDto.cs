namespace Donutsbox.Api.Dto;

/// <summary>
/// DTO комментария к посту
/// </summary>
public class PostCommentDto
{
    /// <summary>
    /// Идентификатор комментария
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Идентификатор поста
    /// </summary>
    public required Guid PostId { get; set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// URL аватарки пользователя
    /// </summary>
    public string? UserAvatarUrl { get; set; }

    /// <summary>
    /// Текст комментария
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Дата создания комментария
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
}
