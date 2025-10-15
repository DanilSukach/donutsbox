namespace Donutsbox.Api.Dto;

public class PostDraftResponseDto
{
    public Guid PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public string Message { get; set; } = string.Empty;
}
