namespace Donutsbox.Api.Dto;

public class PublishPostResponseDto
{
    public Guid PostId { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
