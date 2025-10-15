using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.Kafka;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FilesController(IMinioService minioService, ILogger<FilesController> logger, DonutsboxDbContext db, IMessageProducer kafka) : ControllerBase
{
    /// <summary>
    /// Загружает файл
    /// </summary>
    /// <returns>URL для загрузки файла</returns>
    /// <response code="200">URL получен</response>
    [HttpPost("upload")]
    [RequestSizeLimit(2L * 1024 * 1024 * 1024)] // 2 GB
    public async Task<IActionResult> Upload([FromForm] VideoUploadRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file uploaded");

        var videoId = Guid.NewGuid();
        var objectKey = $"{videoId}/{Path.GetFileName(request.File.FileName)}";

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userId = Guid.Parse(userIdClaim!.Value);

        var video = new Video
        {
            Id = videoId,
            Title = request.Title,
            Description = request.Description,
            UserId = userId!,
            Status = "UPLOADING",
            CreatedAt = DateTime.UtcNow
        };
        db.Videos.Add(video);
        await db.SaveChangesAsync();

        using var stream = request.File.OpenReadStream();
        await minioService.UploadFileAsync(objectKey, stream, request.File.ContentType);

        // 3️⃣ Обновляем статус
        video.Status = "UPLOADED";
        video.ObjectKey = objectKey;
        await db.SaveChangesAsync();

        await kafka.PublishVideoUploadedAsync(new VideoUploadedEvent(video.Id, objectKey));

        logger.LogInformation("Video {VideoId} uploaded and published to Kafka", video.Id);

        return Ok(new { videoId = video.Id });
    }
}
