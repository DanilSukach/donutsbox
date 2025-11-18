using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.Kafka;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FilesController(IMinioService minioService, ILogger<FilesController> logger, DonutsboxDbContext db, IMessageProducer kafka) : ControllerBase
{
    /// <summary>
    /// Загружает видео (только для creator)
    /// </summary>
    [Authorize(Roles = "Creator")]
    [HttpPost("upload")]
    [RequestSizeLimit(2L * 1024 * 1024 * 1024)] // 2 GB
    public async Task<ActionResult<VideoUploadResponseDto>> Upload([FromForm] VideoUploadRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file uploaded");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userId = Guid.Parse(userIdClaim!.Value);

        var user = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        if (user.CreatorPageData == null)
            return BadRequest("Creator page not found");


        var post = await db.ContentPosts.FirstOrDefaultAsync(p =>
            p.Id == request.ContentPostId &&
            p.CreatorPageDataId == user.CreatorPageData.Id);

        if (post == null)
            return BadRequest("Content post not found or does not belong to you");


        var videoId = Guid.NewGuid();
        var objectKey = $"{videoId}/{Path.GetFileName(request.File.FileName)}";

        string? thumbnailUrl = null;
        if (request.Thumbnail != null && request.Thumbnail.Length > 0)
        {
            var thumbnailKey = $"{videoId}/thumbnail_{Path.GetFileName(request.Thumbnail.FileName)}";
            using var thumbStream = request.Thumbnail.OpenReadStream();
            await minioService.UploadFileAsync(thumbnailKey, thumbStream, request.Thumbnail.ContentType);
            thumbnailUrl = thumbnailKey;
            logger.LogInformation("Thumbnail uploaded for video {VideoId}", videoId);
        }

        var video = new Video
        {
            Id = videoId,
            Title = request.Title,
            UserId = userId,
            ContentPostId = request.ContentPostId,
            ContentPost = post,
            Status = "UPLOADING",
            CreatedAt = DateTime.UtcNow,
            ThumbnailUrl = thumbnailUrl 
        };
        db.Videos.Add(video);
        await db.SaveChangesAsync();

        using var stream = request.File.OpenReadStream();
        await minioService.UploadFileAsync(objectKey, stream, request.File.ContentType);

        video.Status = "UPLOADED";
        video.ObjectKey = objectKey;
        await db.SaveChangesAsync();

        await kafka.PublishVideoUploadedAsync(new VideoUploadedEvent(video.Id, objectKey));

        logger.LogInformation("Video {VideoId} uploaded by creator {UserId}", video.Id, userId);

        return Ok(new VideoUploadResponseDto
        {
            VideoId = video.Id,
            Status = video.Status,
            ThumbnailUrl = thumbnailUrl
        });
    }

    /// <summary>
    /// Получить список видео текущего creator'а
    /// </summary>
    [HttpGet("my-videos")]
    public async Task<ActionResult<MyVideoResponseDto>> GetMyVideos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userId = Guid.Parse(userIdClaim!.Value);

        var query = db.Videos
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(v => v.Status == status);

        var total = await query.CountAsync();
        var videos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VideoDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Status = v.Status,
                    CreatedAt = v.CreatedAt,
                    ThumbnailUrl = v.ThumbnailUrl,
                    ProcessedPath = v.ProcessedPath,
                    ContentPostId = v.ContentPostId,
                    HlsUrl = v.ProcessedPath != null ? $"/api/files/{v.Id}/hls/index.m3u8" : null
                })
                .ToListAsync();

        return Ok(new MyVideoResponseDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Videos = videos
        });
    }

    /// <summary>
    /// Получить превью видео
    /// </summary>
    [HttpGet("{videoId:guid}/thumbnail")]
    public async Task<ActionResult<byte[]>> GetThumbnail([FromRoute] Guid videoId)
    {
        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == videoId);
        if (video == null || string.IsNullOrEmpty(video.ThumbnailUrl))
            return NotFound();

        var bytes = await minioService.GetProcessedObjectBytesAsync(video.ThumbnailUrl);
        return File(bytes, "image/jpeg");
    }

    /// <summary>
    /// HLS манифест
    /// </summary>
    [HttpGet("{videoId:guid}/hls/index.m3u8")]
    public async Task<ActionResult<byte[]>> GetManifest([FromRoute] Guid videoId, CancellationToken ct)
    {
        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == videoId, ct);
        if (video == null || string.IsNullOrWhiteSpace(video.ProcessedPath))
            return NotFound();

        var bytes = await minioService.GetProcessedObjectBytesAsync(video.ProcessedPath, ct);
        return File(bytes, "application/vnd.apple.mpegurl");
    }

    [HttpGet("{videoId:guid}/hls/{segment}")]
    public async Task<ActionResult<byte[]>> GetSegment([FromRoute] Guid videoId, [FromRoute] string segment, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(segment) || segment.Contains('/') || segment.Contains('\\'))
            return BadRequest();
        var key = $"processed/{videoId}/{segment}";
        var bytes = await minioService.GetProcessedObjectBytesAsync(key, ct);
        var contentType = segment.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ? "video/mp2t" : "application/octet-stream";
        return File(bytes, contentType);
    }

    [HttpPost("images/avatar")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ImageUploadResponseDto>> UploadAvatar([FromForm] ImageUploadRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { message = "No file" });
        if (!request.File.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Invalid image type" });

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var ext = Path.GetExtension(request.File.FileName);
        var key = $"avatars/{userId}/{Guid.NewGuid()}{ext}";

        using var stream = request.File.OpenReadStream();
        await minioService.UploadImageAsync(key, stream, request.File.ContentType);

        return Ok(new ImageUploadResponseDto { Key = key });
    }

    [HttpPost("images/banner")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImageUploadResponseDto>> UploadBanner([FromForm] ImageUploadRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { message = "No file" });
        if (!request.File.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Invalid image type" });

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var ext = Path.GetExtension(request.File.FileName);
        var key = $"banners/{userId}/{Guid.NewGuid()}{ext}";

        using var stream = request.File.OpenReadStream();
        await minioService.UploadImageAsync(key, stream, request.File.ContentType);

        return Ok(new ImageUploadResponseDto { Key = key });
    }


    [HttpGet("images/url")]
    public async Task<ActionResult<ImageUrlResponseDto>> GetImageUrl([FromQuery] string key, [FromQuery] int ttl = 300)
    {
        if (string.IsNullOrWhiteSpace(key)) return BadRequest();
        var url = await minioService.GetPresignedGetUrlAsync(key, "images", ttl);
        return Ok(new ImageUrlResponseDto { Url = url, TtlSeconds = ttl });
    }

    [Authorize(Roles = "Creator")]
    [HttpPost("images/post")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    public async Task<ActionResult<ImageUploadResponseDto>> UploadPostImage([FromForm] IFormFile file, [FromForm] Guid contentPostId, [FromForm] string? title)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file" });
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Invalid image type" });

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var user = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        if (user.CreatorPageData == null)
            return BadRequest(new { message = "Creator page not found" });

        var post = await db.ContentPosts.FirstOrDefaultAsync(p =>
            p.Id == contentPostId &&
            p.CreatorPageDataId == user.CreatorPageData.Id);

        if (post == null)
            return BadRequest(new { message = "Content post not found or does not belong to you" });

        var imageId = Guid.NewGuid();
        var ext = Path.GetExtension(file.FileName);
        var key = $"content_images/{userId}/{imageId}{ext}";

        using (var stream = file.OpenReadStream())
        {
            await minioService.UploadImageAsync(key, stream, file.ContentType);
        }

        var image = new Image
        {
            Id = imageId,
            UserId = userId,
            Title = title,
            Status = "UPLOADED",
            ObjectKey = key,
            CreatedAt = DateTime.UtcNow,
            ProcessedPath = null,
            ContentPostId = contentPostId,
            ContentPost = post
        };

        db.Images.Add(image);
        await db.SaveChangesAsync();

        logger.LogInformation("Image {ImageId} uploaded by creator {UserId} for post {PostId}", image.Id, userId, contentPostId);

        return Ok(new ImageUploadResponseDto { Key = key });
    }
}
