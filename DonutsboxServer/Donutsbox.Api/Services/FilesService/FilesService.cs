using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.Kafka;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Donutsbox.Api.Services.FilesService;

public class FilesService(
    IMinioService minioService,
    ILogger<FilesService> logger,
    DonutsboxDbContext db,
    IMessageProducer kafka) : IFilesService
{
    public async Task<VideoUploadResponseDto> UploadVideoAsync(Guid userId, VideoUploadRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
            throw new FilesServiceException("No file uploaded", StatusCodes.Status400BadRequest);

        var user = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new FilesServiceException("Unauthorized", StatusCodes.Status401Unauthorized);
        if (user.CreatorPageData == null)
            throw new FilesServiceException("Creator page not found", StatusCodes.Status400BadRequest);

        var post = await db.ContentPosts.FirstOrDefaultAsync(
            p => p.Id == request.ContentPostId && p.CreatorPageDataId == user.CreatorPageData.Id) ?? throw new FilesServiceException("Content post not found or does not belong to you", StatusCodes.Status400BadRequest);
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

        using (var stream = request.File.OpenReadStream())
        {
            await minioService.UploadFileAsync(objectKey, stream, request.File.ContentType);
        }

        video.Status = "UPLOADED";
        video.ObjectKey = objectKey;
        await db.SaveChangesAsync();

        await kafka.PublishVideoUploadedAsync(new VideoUploadedEvent(video.Id, objectKey));

        logger.LogInformation("Video {VideoId} uploaded by creator {UserId}", video.Id, userId);

        var response = new VideoUploadResponseDto
        {
            VideoId = video.Id,
            Status = video.Status,
            ThumbnailUrl = thumbnailUrl
        };

        return response;
    }

    public async Task<MyVideoResponseDto> GetMyVideosAsync(Guid userId, int page, int pageSize, string? status)
    {
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

        return new MyVideoResponseDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Videos = videos
        };
    }

    public async Task<FilePayload> GetThumbnailAsync(Guid videoId)
    {
        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == videoId);
        if (video == null || string.IsNullOrEmpty(video.ThumbnailUrl))
            throw new FilesServiceException("Thumbnail not found", StatusCodes.Status404NotFound);

        var bytes = await minioService.GetProcessedObjectBytesAsync(video.ThumbnailUrl);
        return new FilePayload(bytes, "image/jpeg");
    }

    public async Task<FilePayload> GetManifestAsync(Guid videoId)
    {
        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == videoId);
        if (video == null || string.IsNullOrWhiteSpace(video.ProcessedPath))
            throw new FilesServiceException("Manifest not found", StatusCodes.Status404NotFound);

        var bytes = await minioService.GetProcessedObjectBytesAsync(video.ProcessedPath);
        return new FilePayload(bytes, "application/vnd.apple.mpegurl");
    }

    public async Task<FilePayload> GetSegmentAsync(Guid videoId, string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || segment.Contains('/') || segment.Contains('\\'))
            throw new FilesServiceException("Invalid segment name", StatusCodes.Status400BadRequest);

        var key = $"processed/{videoId}/{segment}";
        var bytes = await minioService.GetProcessedObjectBytesAsync(key);
        var contentType = segment.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ? "video/mp2t" : "application/octet-stream";
        return new FilePayload(bytes, contentType);
    }

    public async Task<ImageUploadResponseDto> UploadAvatarAsync(Guid userId, ImageUploadRequestDto request)
    {
        return await UploadProfileImageAsync(userId, request, "avatars");
    }

    public async Task<ImageUploadResponseDto> UploadBannerAsync(Guid userId, ImageUploadRequestDto request)
    {
        return await UploadProfileImageAsync(userId, request, "banners");
    }

    public async Task<ImageUrlResponseDto> GetImageUrlAsync(string key, int ttl)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new FilesServiceException("Key is required", StatusCodes.Status400BadRequest);

        var url = await minioService.GetPresignedGetUrlAsync(key, "images", ttl);
        return new ImageUrlResponseDto { Url = url, TtlSeconds = ttl };
    }

    public async Task<List<ImageUploadResponseDto>> UploadPostImagesAsync(Guid userId, UploadPostImageDto dto)
    {
        if (dto.Files == null || dto.Files.Count == 0)
            throw new FilesServiceException("No files provided", StatusCodes.Status400BadRequest);

        if (dto.Files.Count > 8)
            throw new FilesServiceException("Maximum 8 images allowed", StatusCodes.Status400BadRequest);

        var invalidFiles = dto.Files.Where(f => f == null || f.Length == 0 || !f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)).ToList();
        if (invalidFiles.Count != 0)
            throw new FilesServiceException("Invalid image files detected", StatusCodes.Status400BadRequest);

        var user = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new FilesServiceException("Unauthorized", StatusCodes.Status401Unauthorized);
        if (user.CreatorPageData == null)
            throw new FilesServiceException("Creator page not found", StatusCodes.Status400BadRequest);

        var post = await db.ContentPosts.FirstOrDefaultAsync(
            p => p.Id == dto.ContentPostId && p.CreatorPageDataId == user.CreatorPageData.Id) ?? throw new FilesServiceException("Content post not found or does not belong to you", StatusCodes.Status400BadRequest);
        var uploadedKeys = new List<ImageUploadResponseDto>();
        var imagesToAdd = new List<Image>();

        foreach (var file in dto.Files)
        {
            var imageId = Guid.NewGuid();
            var ext = Path.GetExtension(file.FileName);
            var key = $"content_images/{userId}/{dto.ContentPostId}/{imageId}{ext}";

            using (var stream = file.OpenReadStream())
            {
                await minioService.UploadImageAsync(key, stream, file.ContentType);
            }

            var image = new Image
            {
                Id = imageId,
                UserId = userId,
                Title = dto.Title,
                Status = "UPLOADED",
                ObjectKey = key,
                CreatedAt = DateTime.UtcNow,
                ProcessedPath = null,
                ContentPostId = dto.ContentPostId,
                ContentPost = post
            };

            imagesToAdd.Add(image);
            uploadedKeys.Add(new ImageUploadResponseDto { Key = key });
            post.Images.Add(key);
        }

        db.Images.AddRange(imagesToAdd);
        await db.SaveChangesAsync();

        logger.LogInformation("{Count} images uploaded by creator {UserId} for post {PostId}",
            uploadedKeys.Count, userId, dto.ContentPostId);

        return uploadedKeys;
    }

    private async Task<ImageUploadResponseDto> UploadProfileImageAsync(Guid userId, ImageUploadRequestDto request, string folder)
    {
        if (request.File == null || request.File.Length == 0)
            throw new FilesServiceException("No file", StatusCodes.Status400BadRequest);

        if (!request.File.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new FilesServiceException("Invalid image type", StatusCodes.Status400BadRequest);

        var ext = Path.GetExtension(request.File.FileName);
        var key = $"{folder}/{userId}/{Guid.NewGuid()}{ext}";

        using (var stream = request.File.OpenReadStream())
        {
            await minioService.UploadImageAsync(key, stream, request.File.ContentType);
        }

        return new ImageUploadResponseDto { Key = key };
    }
}

