namespace Donutsbox.Api.Dto;

public class VideoUploadRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
}
