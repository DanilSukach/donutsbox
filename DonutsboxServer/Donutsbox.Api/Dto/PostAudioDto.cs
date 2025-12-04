namespace Donutsbox.Api.Dto;

public class PostAudioDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProcessedPath { get; set; }
}

