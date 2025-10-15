namespace Donutsbox.Api.Dto;

public class CreatorPostsResponseDto
{
    public CreatorInfoDto Creator { get; set; } = null!;
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<PostDetailsDto> Posts { get; set; } = [];
}
