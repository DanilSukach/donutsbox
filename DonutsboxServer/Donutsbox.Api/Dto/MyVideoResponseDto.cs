namespace Donutsbox.Api.Dto;

public class MyVideoResponseDto
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<VideoDto> Videos { get; set; } = [];
}
