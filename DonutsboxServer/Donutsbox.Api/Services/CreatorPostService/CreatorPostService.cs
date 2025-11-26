using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.Kafka;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Donutsbox.Api.Services.CreatorPostService;

public class CreatorPostService(
    DonutsboxDbContext db,
    IMinioService minio,
    IMessageProducer kafka,
    ILogger<CreatorPostService> logger) : ICreatorPostService
{
    private const string AudiencePublic = "Public";
    private const string AudienceSubscribers = "Subscribers";
    public async Task<PostDraftResponseDto> CreateDraftAsync(CreateDraftRequestDto request, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var currentUser = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new InvalidOperationException("User not found");

        if (currentUser.UserType.Name != "Creator" && currentUser.UserTypeId != 2)
            throw new InvalidOperationException("Only creators can create posts");

        if (currentUser.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        var draftSubscriptionIds = request.SubscriptionIds ?? [];
        var audienceType = DetermineAudienceType(request.IsPublic, draftSubscriptionIds);
        var targetSubscriptions = await LoadCreatorSubscriptionsAsync(currentUser.CreatorPageData.Id, draftSubscriptionIds);
        ValidateAudienceConfiguration(audienceType, targetSubscriptions);

        var post = new ContentPost
        {
            Id = Guid.NewGuid(),
            CreatorPageDataId = currentUser.CreatorPageData.Id,
            CreatorPageData = currentUser.CreatorPageData,
            Title = request.Title,
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow,
            IsPublished = false,
            LikesCount = 0,
            DislikesCount = 0,
            CommentsCount = 0,
            AudienceType = audienceType,
            Subscriptions = targetSubscriptions
        };

        db.ContentPosts.Add(post);
        await db.SaveChangesAsync();

        logger.LogInformation("Creator {UserId} created draft post {PostId}", userId, post.Id);

        return new PostDraftResponseDto
        {
            PostId = post.Id,
            Title = post.Title ?? string.Empty,
            IsPublished = post.IsPublished,
            Message = "Draft created. Upload videos and then publish."
        };
    }

    public async Task<AddVideosResponseDto> AddVideosToPostAsync(Guid postId, AddVideosRequestDto request, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var currentUser = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser?.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        var post = await db.ContentPosts
            .Include(p => p.Videos)
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == currentUser.CreatorPageData.Id) ?? throw new InvalidOperationException("Post not found or doesn't belong to you");
        if (post.IsPublished)
            throw new InvalidOperationException("Cannot add videos to published post");

        await UpdatePostAudienceAsync(post, currentUser.CreatorPageData.Id, request.IsPublic, request.SubscriptionIds ?? []);

        var videos = await db.Videos
            .Where(v => request.VideoIds.Contains(v.Id) && v.UserId == userId)
            .ToListAsync();

        if (videos.Count != request.VideoIds.Count)
            throw new InvalidOperationException("Some videos not found or don't belong to you");

        foreach (var video in videos)
        {
            video.ContentPostId = post.Id;
            video.ContentPost = post;
            post.Videos.Add(video);
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Added {Count} videos to post {PostId}", videos.Count, postId);

        return new AddVideosResponseDto
        {
            PostId = post.Id,
            VideosAdded = videos.Count,
            TotalVideos = post.Videos.Count,
            Message = "Videos added. Ready to publish."
        };
    }

    public async Task<AddImagesResponseDto> AddImagesToPostAsync(Guid postId, AddImagesRequestDto request, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var currentUser = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser?.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        var post = await db.ContentPosts
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == currentUser.CreatorPageData.Id) ?? throw new InvalidOperationException("Post not found or doesn't belong to you");
        if (post.IsPublished)
            throw new InvalidOperationException("Cannot add images to published post");

        var images = await db.Videos
            .Where(v => request.ImageIds.Contains(v.Id) && v.UserId == userId)
            .ToListAsync();

        if (images.Count != request.ImageIds.Count)
            throw new InvalidOperationException("Some images not found or don't belong to you");

        foreach (var image in images)
        {
            image.ContentPostId = post.Id;
            image.ContentPost = post;
            post.Videos.Add(image);
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Added {Count} images to post {PostId}", images.Count, postId);

        return new AddImagesResponseDto
        {
            PostId = post.Id,
            ImagesAdded = images.Count,
            TotalImages = post.Videos.Count,
            Message = "Images added. Ready to publish."
        };
    }

    public async Task<AddTextResponseDto> AddTextToPostAsync(Guid postId, AddTextRequestDto request, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));
        var currentUser = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (currentUser?.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");
        var post = await db.ContentPosts
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == currentUser.CreatorPageData.Id) ?? throw new InvalidOperationException("Post not found or doesn't belong to you");
        post.Title = request.Title;
        post.Text = request.Text;
        await db.SaveChangesAsync();
        logger.LogInformation("Updated title and text for post {PostId}", postId);
        return new AddTextResponseDto
        {
            PostId = post.Id,
            Message = "Post title and text updated successfully."
        };
    }

    public async Task<PublishPostResponseDto> PublishPostAsync(Guid postId, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var currentUser = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser?.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        var post = await db.ContentPosts
            .Include(p => p.Videos)
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == currentUser.CreatorPageData.Id) ?? throw new InvalidOperationException("Post not found or doesn't belong to you");
        if (post.IsPublished)
            throw new InvalidOperationException("Post is already published");

        ValidateAudienceConfiguration(post.AudienceType ?? AudiencePublic, post.Subscriptions);

        post.IsPublished = true;
        post.CreatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        logger.LogInformation("Post {PostId} published by creator {UserId}", postId, userId);

        return new PublishPostResponseDto
        {
            PostId = post.Id,
            IsPublished = post.IsPublished,
            PublishedAt = (DateTimeOffset)post.CreatedAt,
            Message = "Post published successfully!"
        };
    }

    public async Task<MessageResponseDto> UnpublishPostAsync(Guid postId, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var currentUser = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser?.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        var post = await db.ContentPosts
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == currentUser.CreatorPageData.Id) ?? throw new InvalidOperationException("Post not found or doesn't belong to you");
        post.IsPublished = false;

        await db.SaveChangesAsync();

        logger.LogInformation("Post {PostId} unpublished by creator {UserId}", postId, userId);

        return new MessageResponseDto { Message = "Post unpublished" };
    }

    public async Task<MyPostsResponseDto> GetMyPostsAsync(int page, int pageSize, bool? isPublished, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var currentUser = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser?.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        var query = db.ContentPosts
            .Where(p => p.CreatorPageDataId == currentUser.CreatorPageData.Id)
            .Include(p => p.Videos)
            .Include(p => p.Subscriptions)
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
                CommentsCount = p.PostComments.Count,
                Videos = p.Videos.Select(v => new PostVideoDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Status = v.Status,
                    ThumbnailUrl = v.ThumbnailUrl, // Временно сохраняем ключ, потом заменим на presigned URL
                    HlsUrl = v.ProcessedPath != null ? $"/api/files/{v.Id}/hls/index.m3u8" : null
                }).ToList(),
                PictureUrls = new List<string>(), // Инициализируем пустым списком, заполним ниже
                AudienceType = p.AudienceType ?? AudiencePublic,
                SubscriptionIds = p.Subscriptions.Select(s => s.Id).ToList(),
                IsLocked = false,
                LockedMessage = null
            })
            .ToListAsync();

        // Получаем изображения из таблицы Images для каждого поста и генерируем presigned URLs
        var postIds = posts.Select(p => p.Id).ToList();
        var images = await db.Set<Image>()
            .Where(img => postIds.Contains(img.ContentPostId) && !string.IsNullOrWhiteSpace(img.ObjectKey))
            .ToListAsync();

        foreach (var post in posts)
        {
            // Генерируем presigned URLs для превью видео
            foreach (var video in post.Videos)
            {
                if (!string.IsNullOrWhiteSpace(video.ThumbnailUrl))
                {
                    try
                    {
                        var presignedUrl = await minio.GetPresignedGetUrlAsync(video.ThumbnailUrl, minio.GetProcessedBucket(), 300);
                        video.ThumbnailUrl = presignedUrl;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to generate presigned URL for thumbnail {ThumbnailKey}", video.ThumbnailUrl);
                        video.ThumbnailUrl = null; // Убираем превью, если не удалось сгенерировать URL
                    }
                }
            }

            var postImages = images.Where(img => img.ContentPostId == post.Id).ToList();
            var presignedUrls = new List<string>();
            
            foreach (var image in postImages)
            {
                if (!string.IsNullOrWhiteSpace(image.ObjectKey))
                {
                    try
                    {
                        var presignedUrl = await minio.GetPresignedGetUrlAsync(image.ObjectKey, minio.GetImagesBucket(), 300);
                        presignedUrls.Add(presignedUrl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to generate presigned URL for image {ImageKey}", image.ObjectKey);
                        // Пропускаем изображение, если не удалось сгенерировать URL
                    }
                }
            }
            post.PictureUrls = presignedUrls;
        }

        return new MyPostsResponseDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Posts = posts
        };
    }

    public async Task<CreatorPostsResponseDto> GetCreatorPublicPostsAsync(Guid creatorId, ClaimsPrincipal user, int page, int pageSize)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        var creator = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == creatorId);

        if (creator?.CreatorPageData == null)
            throw new InvalidOperationException("Creator not found");

        var isOwner = userId == creatorId;

        IQueryable<ContentPost> query = db.ContentPosts
            .Where(p =>
                p.CreatorPageDataId == creator.CreatorPageData.Id &&
                p.IsPublished == true);

        query = query
            .Include(p => p.Videos.Where(v => v.Status == "READY"))
            .Include(p => p.Subscriptions);

        // Получаем НАЗВАНИЯ активных подписок пользователя (не зависит от срока)
        var viewerSubscriptionNames = new List<string>();
        if (!isOwner)
        {
            var now = DateTime.UtcNow;
            viewerSubscriptionNames = await db.UsersSubscriptions
                .Include(us => us.Subscription)
                .Where(us => us.UserId == userId && us.Status == "active" && us.EndDate >= now)
                .Select(us => us.Subscription.Name)
                .Distinct()
                .ToListAsync();
        }

        query = query.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();
        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostDetailsDto
            {
                Id = p.Id,
                Title = p.Title,
                Text = (isOwner || (p.AudienceType ?? AudiencePublic) == AudiencePublic || p.Subscriptions.Any(s => viewerSubscriptionNames.Contains(s.Name)))
                    ? p.Text
                    : null,
                CreatedAt = (DateTimeOffset)p.CreatedAt!,
                IsPublished = p.IsPublished,
                LikesCount = p.LikesCount,
                DislikesCount = p.DislikesCount,
                CommentsCount = p.PostComments.Count,
                Videos = (isOwner || (p.AudienceType ?? AudiencePublic) == AudiencePublic || p.Subscriptions.Any(s => viewerSubscriptionNames.Contains(s.Name)))
                    ? p.Videos.Select(v => new PostVideoDto
                    {
                        Id = v.Id,
                        Title = v.Title,
                        Status = v.Status,
                        ThumbnailUrl = v.ThumbnailUrl, // Временно сохраняем ключ, потом заменим на presigned URL
                        HlsUrl = $"/api/files/{v.Id}/hls/index.m3u8"
                    }).ToList()
                    : new List<PostVideoDto>(),
                PictureUrls = new List<string>(), // Инициализируем пустым списком, заполним ниже
                ReactionTypeId = p.PostReactions
                                    .Where(pr => pr.UserId == userId)
                                    .Select(pr => (int?)pr.ReactionTypeId)
                                    .FirstOrDefault() ?? 0,
                AudienceType = p.AudienceType ?? AudiencePublic,
                SubscriptionIds = p.Subscriptions.Select(s => s.Id).ToList(),
                IsLocked = !(isOwner ||
                             (p.AudienceType ?? AudiencePublic) == AudiencePublic ||
                             p.Subscriptions.Any(s => viewerSubscriptionNames.Contains(s.Name))),
                LockedMessage = !(isOwner ||
                                  (p.AudienceType ?? AudiencePublic) == AudiencePublic ||
                                  p.Subscriptions.Any(s => viewerSubscriptionNames.Contains(s.Name)))
                    ? "Оформите подписку, чтобы посмотреть этот контент"
                    : null
            })
            .ToListAsync();

        // Получаем изображения из таблицы Images для каждого поста и генерируем presigned URLs
        var postIds = posts.Select(p => p.Id).ToList();
        var images = await db.Set<Image>()
            .Where(img => postIds.Contains(img.ContentPostId) && !string.IsNullOrWhiteSpace(img.ObjectKey))
            .ToListAsync();

        foreach (var post in posts)
        {
            if (post.IsLocked)
            {
                continue;
            }

            // Генерируем presigned URLs для превью видео
            foreach (var video in post.Videos)
            {
                if (!string.IsNullOrWhiteSpace(video.ThumbnailUrl))
                {
                    try
                    {
                        var presignedUrl = await minio.GetPresignedGetUrlAsync(video.ThumbnailUrl, minio.GetProcessedBucket(), 300);
                        video.ThumbnailUrl = presignedUrl;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to generate presigned URL for thumbnail {ThumbnailKey}", video.ThumbnailUrl);
                        video.ThumbnailUrl = null; // Убираем превью, если не удалось сгенерировать URL
                    }
                }
            }

            var postImages = images.Where(img => img.ContentPostId == post.Id).ToList();
            var presignedUrls = new List<string>();
            
            foreach (var image in postImages)
            {
                if (!string.IsNullOrWhiteSpace(image.ObjectKey))
                {
                    try
                    {
                        var presignedUrl = await minio.GetPresignedGetUrlAsync(image.ObjectKey, minio.GetImagesBucket(), 300);
                        presignedUrls.Add(presignedUrl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to generate presigned URL for image {ImageKey}", image.ObjectKey);
                        // Пропускаем изображение, если не удалось сгенерировать URL
                    }
                }
            }
            post.PictureUrls = presignedUrls;
        }

        return new CreatorPostsResponseDto
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
        };
    }

    public async Task<MessageResponseDto> DeletePostAsync(Guid postId, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var currentUser = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser?.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        var post = await db.ContentPosts
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p =>
                p.Id == postId &&
                p.CreatorPageDataId == currentUser.CreatorPageData.Id) ?? throw new InvalidOperationException("Post not found or doesn't belong to you");
        logger.LogInformation("Starting deletion of post {PostId} with {VideoCount} videos and {ImageCount} images",
            post.Id, post.Videos.Count, post.Images.Count);

        foreach (var video in post.Videos)
        {
            try
            {
                await minio.DeleteDirectoryAsync($"{video.Id}/", minio.GetTempBucket());
                await minio.DeleteDirectoryAsync($"processed/{video.Id}/", minio.GetProcessedBucket());
                logger.LogDebug("Deleted files for video {VideoId}", video.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete MinIO files for video {VideoId}", video.Id);
            }

            db.Videos.Remove(video);
        }

        foreach (var pictureUrl in post.Images)
        {
            if (!string.IsNullOrEmpty(pictureUrl))
            {
                try
                {
                    await minio.DeleteObjectAsync(pictureUrl, minio.GetImagesBucket());
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete image {ImageUrl}", pictureUrl);
                }
            }
        }

        db.ContentPosts.Remove(post);
        await db.SaveChangesAsync();

        logger.LogInformation("Successfully deleted post {PostId} with {VideoCount} videos and {ImageCount} images",
            post.Id, post.Videos.Count, post.Images.Count);

        return new MessageResponseDto { Message = "Post and all media files deleted successfully" };
    }

    public async Task<UploadImagesResponseDto> UploadImagesAsync(UploadImagesRequestDto request, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var currentUser = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser?.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        if (currentUser.UserType.Name != "Creator")
            throw new InvalidOperationException("Only creators can upload images");

        if (request.Images == null || request.Images.Count == 0)
            throw new InvalidOperationException("No images provided");

        var uploadedUrls = new List<string>();

        foreach (var image in request.Images)
        {
            if (image.Length == 0) continue;

            var imageId = Guid.NewGuid();
            var extension = Path.GetExtension(image.FileName);
            var objectKey = $"posts/{currentUser.CreatorPageData.Id}/{imageId}{extension}";

            using var stream = image.OpenReadStream();
            await minio.UploadImageAsync(objectKey, stream, image.ContentType);

            uploadedUrls.Add(objectKey);
        }

        logger.LogInformation("Creator {UserId} uploaded {Count} images", userId, uploadedUrls.Count);

        return new UploadImagesResponseDto { ImageUrls = uploadedUrls };
    }

    public async Task<byte[]> GetImageAsync(string imagePath)
    {
        try
        {
            var bytes = await minio.GetProcessedObjectBytesAsync(imagePath);
            logger.LogDebug("Retrieved image {ImagePath}", imagePath);
            return bytes;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve image {ImagePath}", imagePath);
            throw new InvalidOperationException("Image not found");
        }
    }

    public async Task<MyPostsResponseDto> GetSubscriptionFeedAsync(int page, int pageSize, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var now = DateTime.UtcNow;
        var userSubscriptions = await db.Set<UserSubscription>()
            .Include(us => us.Subscription)
                .ThenInclude(s => s.CreatorPageData)
            .Where(us => us.UserId == userId && us.Status == "active" && us.EndDate >= now)
            .ToListAsync();

        if (userSubscriptions.Count == 0)
        {
            return new MyPostsResponseDto
            {
                Total = 0,
                Page = page,
                PageSize = pageSize,
                Posts = []
            };
        }

        var creatorPageIds = userSubscriptions
            .Select(us => us.Subscription.CreatorPageDataId)
            .Distinct()
            .ToList();

        var userSubscriptionNames = userSubscriptions
            .Select(us => us.Subscription.Name)
            .Distinct()
            .ToList();

        IQueryable<ContentPost> query = db.ContentPosts
            .Where(p => creatorPageIds.Contains(p.CreatorPageDataId) && p.IsPublished == true);

        query = query
            .Include(p => p.Videos.Where(v => v.Status == "READY"))
            .Include(p => p.CreatorPageData)
                .ThenInclude(cpd => cpd.User)
            .Include(p => p.Subscriptions);

        query = query.Where(p =>
            (p.AudienceType ?? AudiencePublic) == AudiencePublic ||
            p.Subscriptions.Any(s => userSubscriptionNames.Contains(s.Name)));

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
                CommentsCount = p.PostComments.Count,
                Videos = p.Videos.Select(v => new PostVideoDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Status = v.Status,
                    ThumbnailUrl = v.ThumbnailUrl,
                    HlsUrl = v.ProcessedPath != null ? $"/api/files/{v.Id}/hls/index.m3u8" : null
                }).ToList(),
                PictureUrls = new List<string>(), 
                CreatorPageName = p.CreatorPageData.PageName,
                CreatorId = p.CreatorPageData.UserId,
                CreatorAvatarUrl = p.CreatorPageData.AvatarURL,
                ReactionTypeId = p.PostReactions
                                    .Where(pr => pr.UserId == userId)
                                    .Select(pr => (int?)pr.ReactionTypeId)
                                    .FirstOrDefault() ?? 0,
                AudienceType = p.AudienceType ?? AudiencePublic,
                SubscriptionIds = p.Subscriptions.Select(s => s.Id).ToList(),
                IsLocked = false,
                LockedMessage = null
            })
            .ToListAsync();

        var postIds = posts.Select(p => p.Id).ToList();
        var images = await db.Set<Image>()
            .Where(img => postIds.Contains(img.ContentPostId) && !string.IsNullOrWhiteSpace(img.ObjectKey))
            .ToListAsync();

        foreach (var post in posts)
        {
            foreach (var video in post.Videos)
            {
                if (!string.IsNullOrWhiteSpace(video.ThumbnailUrl))
                {
                    try
                    {
                        var presignedUrl = await minio.GetPresignedGetUrlAsync(video.ThumbnailUrl, minio.GetProcessedBucket(), 300);
                        video.ThumbnailUrl = presignedUrl;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to generate presigned URL for thumbnail {ThumbnailKey}", video.ThumbnailUrl);
                        video.ThumbnailUrl = null; 
                    }
                }
            }

            var postImages = images.Where(img => img.ContentPostId == post.Id).ToList();
            var presignedUrls = new List<string>();
            
            foreach (var image in postImages)
            {
                if (!string.IsNullOrWhiteSpace(image.ObjectKey))
                {
                    try
                    {
                        var presignedUrl = await minio.GetPresignedGetUrlAsync(image.ObjectKey, minio.GetImagesBucket(), 300);
                        presignedUrls.Add(presignedUrl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to generate presigned URL for image {ImageKey}", image.ObjectKey);
                    }
                }
            }
            post.PictureUrls = presignedUrls;

            if (!post.IsLocked && !string.IsNullOrEmpty(post.CreatorAvatarUrl))
            {
                try
                {
                    post.CreatorAvatarUrl = await minio.GetPresignedGetUrlAsync(
                        post.CreatorAvatarUrl,
                        minio.GetImagesBucket(),
                        300
                    );
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to get avatar URL for creator {CreatorId}", post.CreatorId);
                    post.CreatorAvatarUrl = null;
                }
            }
        }

        logger.LogInformation("User {UserId} requested feed: {PostCount} posts from {CreatorCount} creators",
            userId, posts.Count, creatorPageIds.Count);

        return new MyPostsResponseDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Posts = posts
        };
    }

    private static string DetermineAudienceType(bool? isPublic, List<Guid> subscriptionIds, string? fallback = AudiencePublic)
    {
        if (isPublic.HasValue)
            return isPublic.Value ? AudiencePublic : AudienceSubscribers;

        if (subscriptionIds.Count > 0)
            return AudienceSubscribers;

        return string.IsNullOrWhiteSpace(fallback) ? AudiencePublic : fallback!;
    }

    private async Task<List<Subscription>> LoadCreatorSubscriptionsAsync(Guid creatorPageDataId, List<Guid> subscriptionIds)
    {
        if (subscriptionIds.Count == 0)
            return [];

        var selectedSubscriptions = await db.Subscriptions
            .Where(s => s.CreatorPageDataId == creatorPageDataId && subscriptionIds.Contains(s.Id))
            .ToListAsync();

        if (selectedSubscriptions.Count != subscriptionIds.Count)
            throw new InvalidOperationException("Subscription list contains invalid entries");

        var subscriptionNames = selectedSubscriptions.Select(s => s.Name).Distinct().ToList();

        var allSubscriptionsWithSameNames = await db.Subscriptions
            .Where(s => s.CreatorPageDataId == creatorPageDataId && subscriptionNames.Contains(s.Name))
            .ToListAsync();

        logger.LogInformation(
            "Loaded {Count} subscriptions (all periods) for creator {CreatorId} with names: {Names}",
            allSubscriptionsWithSameNames.Count,
            creatorPageDataId,
            string.Join(", ", subscriptionNames));

        return allSubscriptionsWithSameNames;
    }

    private static void ValidateAudienceConfiguration(string audienceType, List<Subscription>? subscriptions)
    {
        if ((audienceType ?? AudiencePublic) == AudienceSubscribers && (subscriptions == null || subscriptions.Count == 0))
            throw new InvalidOperationException("Выберите хотя бы одну подписку для ограниченного поста");
    }

    private async Task UpdatePostAudienceAsync(ContentPost post, Guid creatorPageDataId, bool? isPublic, List<Guid> subscriptionIds)
    {
        var shouldUpdateAudience = isPublic.HasValue || subscriptionIds.Count > 0;
        if (!shouldUpdateAudience)
            return;

        var audienceType = DetermineAudienceType(isPublic, subscriptionIds, post.AudienceType);
        List<Subscription> newSubscriptions = [];

        if (audienceType == AudienceSubscribers)
        {
            newSubscriptions = await LoadCreatorSubscriptionsAsync(creatorPageDataId, subscriptionIds);
        }

        post.Subscriptions.Clear();
        foreach (var subscription in newSubscriptions)
        {
            post.Subscriptions.Add(subscription);
        }

        ValidateAudienceConfiguration(audienceType, post.Subscriptions);
        post.AudienceType = audienceType;
    }

    public async Task<MessageResponseDto> CancelVideoProcessingAsync(Guid videoId, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var video = await db.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId) ?? throw new InvalidOperationException("Video not found");
        if (video.UserId != userId)
        {
            throw new InvalidOperationException("Access denied");
        }

        var status = video.Status.ToUpperInvariant();
        if (status != "PENDING" && status != "PROCESSING" && status != "UPLOADED")
        {
            throw new InvalidOperationException($"Cannot cancel video with status: {video.Status}");
        }

        video.Status = "CANCELLED";
        await db.SaveChangesAsync();

        await kafka.PublishVideoProcessingCancelledAsync(new VideoProcessingCancelledEvent(
            videoId,
            "Cancelled by user"
        ));

        logger.LogInformation("Video {VideoId} processing cancelled by user {UserId}", videoId, userId);

        return new MessageResponseDto
        {
            Message = "Video processing cancelled"
        };
    }

    public async Task<MessageResponseDto> DeleteVideoAsync(Guid videoId, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var video = await db.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId) ?? throw new InvalidOperationException("Video not found");
        if (video.UserId != userId)
        {
            throw new InvalidOperationException("Access denied");
        }

        var status = video.Status.ToUpperInvariant();
        if (status == "PENDING" || status == "PROCESSING" || status == "UPLOADED")
        {
            await kafka.PublishVideoProcessingCancelledAsync(new VideoProcessingCancelledEvent(
                videoId,
                "Video deleted by user"
            ));
        }

        db.Videos.Remove(video);
        await db.SaveChangesAsync();

        logger.LogInformation("Video {VideoId} deleted by user {UserId}", videoId, userId);

        return new MessageResponseDto
        {
            Message = "Video deleted"
        };
    }

    public async Task<MessageResponseDto> DeleteImageAsync(string imageKey, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

        var image = await db.Images
            .FirstOrDefaultAsync(i => i.ObjectKey == imageKey) ?? throw new InvalidOperationException("Image not found");
        if (image.UserId != userId)
        {
            throw new InvalidOperationException("Access denied");
        }

        db.Images.Remove(image);
        await db.SaveChangesAsync();


        logger.LogInformation("Image {ImageKey} deleted by user {UserId}", imageKey, userId);

        return new MessageResponseDto
        {
            Message = "Image deleted"
        };
    }
}
