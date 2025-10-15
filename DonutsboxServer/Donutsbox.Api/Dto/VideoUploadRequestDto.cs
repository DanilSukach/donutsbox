namespace Donutsbox.Api.Dto;

public class VideoUploadRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;

    public IFormFile? Thumbnail { get; set; }
    public required Guid ContentPostId { get; set; }
}
