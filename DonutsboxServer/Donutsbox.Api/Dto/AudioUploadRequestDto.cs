namespace Donutsbox.Api.Dto;

public class AudioUploadRequestDto
{
    public string Title { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
    public required Guid ContentPostId { get; set; }
}

