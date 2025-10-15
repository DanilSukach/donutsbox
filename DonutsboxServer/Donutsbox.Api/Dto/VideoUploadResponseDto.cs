namespace Donutsbox.Api.Dto;

public class VideoUploadResponseDto
{
    public Guid VideoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}
