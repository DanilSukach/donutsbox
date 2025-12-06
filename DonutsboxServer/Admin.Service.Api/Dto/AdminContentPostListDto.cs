namespace Admin.Service.Api.Dto;

/// <summary>
/// DTO для отображения поста в админ-панели
/// </summary>
public class AdminContentPostListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Guid CreatorPageDataId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public int LikesCount { get; set; }
    public int DislikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int MediaCount { get; set; }
    public bool IsShadowBanned { get; set; }
}
