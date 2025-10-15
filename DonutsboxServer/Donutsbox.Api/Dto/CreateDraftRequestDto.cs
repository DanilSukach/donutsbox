namespace Donutsbox.Api.Dto;

public class CreateDraftRequestDto
{
    public string? Title { get; set; }
    public string? Text { get; set; }
    public List<string>? PictureUrls { get; set; }
    public List<string>? AudioUrls { get; set; }
}
