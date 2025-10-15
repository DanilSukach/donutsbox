namespace Donutsbox.Api.Dto;

public class AddVideosResponseDto
{
    public Guid PostId { get; set; }
    public int VideosAdded { get; set; }
    public int TotalVideos { get; set; }
    public string Message { get; set; } = string.Empty;
}
