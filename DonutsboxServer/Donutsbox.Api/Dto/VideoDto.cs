namespace Donutsbox.Api.Dto;

public class VideoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ProcessedPath { get; set; }
    public Guid? ContentPostId { get; set; }
    public string? HlsUrl { get; set; }
}
