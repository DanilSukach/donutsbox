using Donutsbox.Api.Dto;
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
[Authorize]
public class CreatorPostController(
    DonutsboxDbContext db,
    IMinioService minio,
    ILogger<CreatorPostController> logger) : ControllerBase
{
    /// <summary>
    /// Шаг 1: Создать черновик поста (не опубликован)
    /// </summary>
    [HttpPost("draft")]
    public async Task<ActionResult<PostDraftResponseDto>> CreateDraft([FromBody] CreateDraftRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var user = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        if (user.UserType.Name != "Creator" && user.UserTypeId != 2)
            return Forbid("Only creators can create posts");

        if (user.CreatorPageData == null)
            return BadRequest("Creator page not found");

        // Создаём черновик (IsPublished = false)
        var post = new ContentPost
        {
            Id = Guid.NewGuid(),
            CreatorPageDataId = user.CreatorPageData.Id,
            CreatorPageData = user.CreatorPageData,
            Title = request.Title,
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow,
            IsPublished = false,  // ✅ Черновик
            LikesCount = 0,
            DislikesCount = 0,
            CommentsCount = 0,
        };

        db.ContentPosts.Add(post);
        await db.SaveChangesAsync();

        logger.LogInformation("Creator {UserId} created draft post {PostId}", userId, post.Id);

        return Ok(new PostDraftResponseDto
        {
            PostId = post.Id,
            Title = post.Title ?? string.Empty,
            IsPublished = post.IsPublished,
            Message = "Draft created. Upload videos and then publish."
        });
    }
    /// <summary>
    /// Шаг 2: Добавить видео к черновику поста
    /// </summary>
    [HttpPost("{postId:guid}/videos")]
    public async Task<ActionResult<AddVideosResponseDto>> AddVideosToPost(
        [FromRoute] Guid postId,
        [FromBody] AddVideosRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var user = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.CreatorPageData == null)
            return Forbid();

        // Проверяем что пост принадлежит creator'у
        var post = await db.ContentPosts
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == user.CreatorPageData.Id);

        if (post == null)
            return NotFound("Post not found or doesn't belong to you");

        if (post.IsPublished)
            return BadRequest("Cannot add videos to published post");

        // Проверяем что все видео принадлежат пользователю
        var videos = await db.Videos
            .Where(v => request.VideoIds.Contains(v.Id) && v.UserId == userId)
            .ToListAsync();

        if (videos.Count != request.VideoIds.Count)
            return BadRequest("Some videos not found or don't belong to you");

        // Привязываем видео к посту
        foreach (var video in videos)
        {

            video.ContentPostId = post.Id;
            video.ContentPost = post;
            post.Videos.Add(video);

        }

        await db.SaveChangesAsync();

        logger.LogInformation("Added {Count} videos to post {PostId}", videos.Count, postId);

        return Ok(new AddVideosResponseDto
        {
            PostId = post.Id,
            VideosAdded = videos.Count,
            TotalVideos = post.Videos.Count,
            Message = "Videos added. Ready to publish."
        });
    }

    /// <summary>
    /// Шаг 3: Опубликовать пост (сделать видимым)
    /// </summary>
    [HttpPost("{postId:guid}/publish")]
    public async Task<ActionResult<PublishPostResponseDto>> PublishPost([FromRoute] Guid postId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var user = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.CreatorPageData == null)
            return Forbid();

        var post = await db.ContentPosts
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == user.CreatorPageData.Id);

        if (post == null)
            return NotFound("Post not found or doesn't belong to you");

        if (post.IsPublished)
            return BadRequest("Post is already published");

        // Публикуем пост
        post.IsPublished = true;
        post.CreatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        logger.LogInformation("Post {PostId} published by creator {UserId}", postId, userId);

        return Ok(new PublishPostResponseDto
        {
            PostId = post.Id,
            IsPublished = post.IsPublished,
            PublishedAt = (DateTimeOffset)post.CreatedAt,
            Message = "Post published successfully!"
        });
    }

    /// <summary>
    /// Снять пост с публикации (вернуть в черновики)
    /// </summary>
    [HttpPost("{postId:guid}/unpublish")]
    public async Task<ActionResult<MessageResponseDto>> UnpublishPost([FromRoute] Guid postId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var user = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.CreatorPageData == null)
            return Forbid();

        var post = await db.ContentPosts
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == user.CreatorPageData.Id);

        if (post == null)
            return NotFound();

        post.IsPublished = false;
        post.CreatedAt = null;

        await db.SaveChangesAsync();

        return Ok(new MessageResponseDto { Message = "Post unpublished" });
    }

    /// <summary>
    /// Получить свои посты (опубликованные и черновики)
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<MyPostsResponseDto>> GetMyPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isPublished = null)  
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var user = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.CreatorPageData == null)
            return BadRequest("Creator page not found");

        var query = db.ContentPosts
            .Where(p => p.CreatorPageDataId == user.CreatorPageData.Id)
            .Include(p => p.Videos)
            .AsQueryable();

        if (isPublished.HasValue)
            query = query.Where(p => p.IsPublished == isPublished.Value);

        query = query.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();
        var posts = await query
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .Select(p => new PostDetailsDto
          {
              Id = p.Id,
              Title = p.Title,
              Text = p.Text,
              CreatedAt = (DateTimeOffset)p.CreatedAt!,
              IsPublished = p.IsPublished,
              LikesCount = p.LikesCount,
              DislikesCount = p.DislikesCount,
              CommentsCount = p.CommentsCount,
              Videos = p.Videos.Select(v => new PostVideoDto  // ✅ Явный тип
              {
                  Id = v.Id,
                  Title = v.Title,
                  Status = v.Status,
                  ThumbnailUrl = v.ThumbnailUrl != null ? $"/api/files/{v.Id}/thumbnail" : null,
                  HlsUrl = v.ProcessedPath != null ? $"/api/files/{v.Id}/hls/index.m3u8" : null
              }).ToList(),
              PictureUrls = p.PictureURLs.Select(url => $"/api/creator/posts/images/{url}").ToList()
          })
          .ToListAsync();

        return Ok(new MyPostsResponseDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Posts = posts
        });
    }

    /// <summary>
    /// Получить публичные посты creator'а (только опубликованные, для фронтенда)
    /// </summary>
    [HttpGet("creator/{creatorId:guid}")]
    public async Task<ActionResult<CreatorPostsResponseDto>> GetCreatorPublicPosts(
        [FromRoute] Guid creatorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var creator = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == creatorId);

        if (creator?.CreatorPageData == null)
            return NotFound("Creator not found");

        var query = db.ContentPosts
            .Where(p =>
                p.CreatorPageDataId == creator.CreatorPageData.Id &&
                p.IsPublished == true)
            .Include(p => p.Videos.Where(v => v.Status == "READY"))
            .OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();
        var posts = await query
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .Select(p => new PostDetailsDto
               {
                   Id = p.Id,
                   Title = p.Title,
                   Text = p.Text,
                   CreatedAt = (DateTimeOffset)p.CreatedAt!,
                   IsPublished = p.IsPublished,
                   LikesCount = p.LikesCount,
                   DislikesCount = p.DislikesCount,
                   CommentsCount = p.CommentsCount,
                   Videos = p.Videos.Select(v => new PostVideoDto
                   {
                       Id = v.Id,
                       Title = v.Title,
                       Status = v.Status,
                       ThumbnailUrl = v.ThumbnailUrl != null ? $"/api/files/{v.Id}/thumbnail" : null,
                       HlsUrl = $"/api/files/{v.Id}/hls/index.m3u8"
                   }).ToList(),
                   PictureUrls = p.PictureURLs.Select(url => $"/api/creator/posts/images/{url}").ToList()
               })
               .ToListAsync();

        return Ok(new CreatorPostsResponseDto
        {
            Creator = new CreatorInfoDto
            {
                Id = creator.Id,
                Name = creator.Name,
                PageName = creator.CreatorPageData.PageName,
                AvatarUrl = creator.CreatorPageData.AvatarURL,
                Description = creator.CreatorPageData.Description,
                SubscribersCount = creator.CreatorPageData.SubscribersCount
            },
            Total = total,
            Page = page,
            PageSize = pageSize,
            Posts = posts
        });
    }

    /// <summary>
    /// Загрузить картинки для поста
    /// </summary>
    [HttpPost("upload-images")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<UploadImagesResponseDto>> UploadImages([FromForm] UploadImageRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var user = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.CreatorPageData == null || user.UserType.Name != "Creator")
            return Forbid();

        if (request.Images == null || request.Images.Count == 0)
            return BadRequest("No images provided");

        var uploadedUrls = new List<string>();

        foreach (var image in request.Images)
        {
            if (image.Length == 0) continue;

            var imageId = Guid.NewGuid();
            var extension = Path.GetExtension(image.FileName);
            var objectKey = $"posts/{user.CreatorPageData.Id}/{imageId}{extension}";

            using var stream = image.OpenReadStream();
            await minio.UploadFileAsync(objectKey, stream, image.ContentType);

            uploadedUrls.Add(objectKey);
        }

        return Ok(new UploadImagesResponseDto { ImageUrls = uploadedUrls });
    }

    /// <summary>
    /// Получить изображение поста
    /// </summary>
    [HttpGet("images/{*imagePath}")]
    public async Task<ActionResult<byte[]>> GetImage(string imagePath)
    {
        try
        {
            var bytes = await minio.GetProcessedObjectBytesAsync(imagePath);
            return File(bytes, "image/jpeg");
        }
        catch
        {
            return NotFound();
        }
    }
}
