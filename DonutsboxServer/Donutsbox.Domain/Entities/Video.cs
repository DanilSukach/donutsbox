namespace Donutsbox.Domain.Entities;

public class Video
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "PENDING"; // PENDING, UPLOADED, PROCESSING, READY, FAILED
    public string ObjectKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ProcessedPath { get; set; }
    public string? ThumbnailUrl { get; set; }
    public required Guid ContentPostId { get; set; }
    public required ContentPost ContentPost { get; set; }
}
