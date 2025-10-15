namespace Donutsbox.Api.Dto;

public class AuthorPreviewDto
{
    /// <summary>
    /// Идентификатор автора
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Ник автора/страницы
    /// </summary>
    public string PageName { get; set; } = null!;
    /// <summary>
    /// Ссылка на аватар автора
    /// </summary>
    public string AvatarUrl { get; set; } = null!;
}
