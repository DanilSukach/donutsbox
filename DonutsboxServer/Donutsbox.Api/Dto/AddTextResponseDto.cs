namespace Donutsbox.Api.Dto;

public class AddTextResponseDto
{
    public Guid PostId { get; set; }
    public string Message { get; set; } = string.Empty;
}
