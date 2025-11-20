namespace Donutsbox.Api.Dto;

public class AddImagesResponseDto
{
    public Guid PostId { get; set; }
    public int ImagesAdded { get; set; }
    public int TotalImages { get; set; }
    public string Message { get; set; } = string.Empty;
}
