using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Dto;

public class UploadPostImageDto
{
    public required List<IFormFile> Files { get; set; }
    public required Guid ContentPostId { get; set; }
}
