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
        if (request.File == null || request.File.Length == 0)
            throw new FilesServiceException("No file", StatusCodes.Status400BadRequest);

        if (!request.File.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new FilesServiceException("Invalid image type", StatusCodes.Status400BadRequest);

        var user = await db.Users
            .Include(u => u.UserData)
            .FirstOrDefaultAsync(u => u.Id == userId) 
            ?? throw new FilesServiceException("User not found", StatusCodes.Status404NotFound);

        var ext = Path.GetExtension(request.File.FileName);
        var key = $"avatars/{userId}/{Guid.NewGuid()}{ext}";

        using (var stream = request.File.OpenReadStream())
        {
            await minioService.UploadImageAsync(key, stream, request.File.ContentType);
        }

        // Создаём UserData если её нет, или обновляем AvatarUrl
        if (user.UserData == null)
        {
            user.UserData = new UserData
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = user,
                AvatarUrl = key
            };
            db.UsersData.Add(user.UserData);
        }
        else
        {
            user.UserData.AvatarUrl = key;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Avatar uploaded for user {UserId}: {Key}", userId, key);

        return new ImageUploadResponseDto { Key = key };
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
                Title = string.Empty,
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

    public async Task<AudioUploadResponseDto> UploadAudioAsync(Guid userId, AudioUploadRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
            throw new FilesServiceException("No file uploaded", StatusCodes.Status400BadRequest);

        // Валидация типа файла
        var allowedContentTypes = new[] { 
            "audio/mpeg", 
            "audio/mp3", 
            "audio/mpeg3",
            "audio/x-mpeg-3",
            "audio/x-mpeg",
            "audio/wav", 
            "audio/wave",
            "audio/x-wav",
            "audio/x-pn-wav",
            "audio/ogg", 
            "audio/vorbis",
            "audio/webm",
            "audio/x-m4a",
            "audio/mp4",
            "audio/aac",
            "audio/x-aac",
            "application/octet-stream" // Для файлов без определенного MIME типа
        };
        
        var contentType = request.File.ContentType?.ToLowerInvariant() ?? string.Empty;
        var fileName = request.File.FileName ?? string.Empty;
        
        // Проверяем расширение файла
        var hasValidExtension = fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                               fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                               fileName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                               fileName.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
                               fileName.EndsWith(".aac", StringComparison.OrdinalIgnoreCase) ||
                               fileName.EndsWith(".webm", StringComparison.OrdinalIgnoreCase);
        
        // Проверяем MIME-тип (может быть пустым или неправильным)
        var hasValidContentType = string.IsNullOrEmpty(contentType) || 
                                 allowedContentTypes.Any(ct => contentType.Contains(ct, StringComparison.OrdinalIgnoreCase)) ||
                                 contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
        
        // Если есть валидное расширение, разрешаем загрузку (даже если MIME-тип неправильный)
        // Или если MIME-тип валидный
        if (!hasValidExtension && !hasValidContentType)
        {
            logger.LogWarning("Invalid audio file type. ContentType: {ContentType}, FileName: {FileName}", contentType, fileName);
            throw new FilesServiceException($"Invalid audio file type. ContentType: {contentType}, FileName: {fileName}. Supported: MP3, WAV, OGG, M4A, AAC, WEBM", StatusCodes.Status400BadRequest);
        }
        
        // Логируем для отладки
        logger.LogInformation("Audio file validation passed. ContentType: {ContentType}, FileName: {FileName}", contentType, fileName);

        var user = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new FilesServiceException("Unauthorized", StatusCodes.Status401Unauthorized);
        if (user.CreatorPageData == null)
            throw new FilesServiceException("Creator page not found", StatusCodes.Status400BadRequest);

        var post = await db.ContentPosts.FirstOrDefaultAsync(
            p => p.Id == request.ContentPostId && p.CreatorPageDataId == user.CreatorPageData.Id) ?? throw new FilesServiceException("Content post not found or does not belong to you", StatusCodes.Status400BadRequest);

        var audioId = Guid.NewGuid();
        var ext = Path.GetExtension(request.File.FileName);
        var objectKey = $"{audioId}/{Path.GetFileName(request.File.FileName)}";

        var audio = new Audio
        {
            Id = audioId,
            Title = request.Title,
            UserId = userId,
            ContentPostId = request.ContentPostId,
            ContentPost = post,
            Status = "UPLOADING",
            CreatedAt = DateTime.UtcNow
        };

        db.Audios.Add(audio);
        await db.SaveChangesAsync();

        using (var stream = request.File.OpenReadStream())
        {
            await minioService.UploadAudioAsync(objectKey, stream, request.File.ContentType);
        }

        audio.Status = "UPLOADED";
        audio.ObjectKey = objectKey;
        await db.SaveChangesAsync();

        await kafka.PublishAudioUploadedAsync(new AudioUploadedEvent(audio.Id, objectKey));

        logger.LogInformation("Audio {AudioId} uploaded by creator {UserId}", audio.Id, userId);

        var response = new AudioUploadResponseDto
        {
            AudioId = audio.Id,
            Status = audio.Status
        };

        return response;
    }

    public async Task<AudioUrlResponseDto> GetAudioUrlAsync(string key, int ttl)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new FilesServiceException("Key is required", StatusCodes.Status400BadRequest);

        var audioBucket = minioService.GetAudioBucket();
        var url = await minioService.GetPresignedGetUrlAsync(key, audioBucket, ttl);
        return new AudioUrlResponseDto { Url = url, TtlSeconds = ttl };
    }

    public async Task<MessageResponseDto> DeleteAudioAsync(Guid audioId, Guid userId)
    {
        var audio = await db.Audios
            .FirstOrDefaultAsync(a => a.Id == audioId) 
            ?? throw new FilesServiceException("Audio not found", StatusCodes.Status404NotFound);

        if (audio.UserId != userId)
            throw new FilesServiceException("Access denied", StatusCodes.Status403Forbidden);

        // Удаляем файлы из MinIO
        try
        {
            var audioBucket = minioService.GetAudioBucket();
            if (!string.IsNullOrEmpty(audio.ObjectKey))
            {
                await minioService.DeleteObjectAsync(audio.ObjectKey, audioBucket);
            }
            if (!string.IsNullOrEmpty(audio.ProcessedPath))
            {
                var processedBucket = minioService.GetProcessedBucket();
                await minioService.DeleteObjectAsync(audio.ProcessedPath, processedBucket);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete MinIO files for audio {AudioId}", audioId);
            // Продолжаем удаление из БД даже если файлы не удалились
        }

        db.Audios.Remove(audio);
        await db.SaveChangesAsync();

        logger.LogInformation("Audio {AudioId} deleted by user {UserId}", audioId, userId);

        return new MessageResponseDto { Message = "Audio deleted successfully" };
    }
}

