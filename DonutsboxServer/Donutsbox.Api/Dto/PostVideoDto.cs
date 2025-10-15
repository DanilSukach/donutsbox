namespace Donutsbox.Api.Dto;

public class PostVideoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? HlsUrl { get; set; }
}
