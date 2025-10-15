namespace Donutsbox.Api.Dto;

public class PostDetailsDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsPublished { get; set; }
    public int LikesCount { get; set; }
    public int DislikesCount { get; set; }
    public int CommentsCount { get; set; }
    public List<PostVideoDto> Videos { get; set; } = [];
    public List<string> PictureUrls { get; set; } = [];
}
