namespace Donutsbox.Api.Dto;

public class ImageUrlResponseDto
{
    public required string Url { get; set; }
    public int TtlSeconds { get; set; }
}