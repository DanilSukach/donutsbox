using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Domain.Repositories.UserSubscriptionsRepository;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Donutsbox.Api.Services.UserSubscriptionsService;

public class UserSubscriptionsService(
    IUserSubscriptionsRepository userRepository,
    IMinioService minioService,
    ILogger<UserSubscriptionsService> logger) : IUserSubscriptionsService
{
    public async Task<IEnumerable<AuthorPreviewDto>> GetAuthorPagesFromUserSubscribes(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await userRepository.GetByIdUserWithSubscriptionsAsync(userId);

        if (userEntity == null)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var authorPages = userEntity.UserSubscriptions
            .Where(us => string.Equals(us.Status, "active", StringComparison.OrdinalIgnoreCase) && us.EndDate >= now)
            .Select(us => us.Subscription)
            .Select(s => s.CreatorPageData)
            .Where(page => page != null)
            .ToList();

        var authorsPreviews = new List<AuthorPreviewDto>(authorPages.Count);

        foreach (var page in authorPages)
        {
            var avatarUrl = page.User?.UserData?.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                try
                {
                    avatarUrl = await minioService.GetPresignedGetUrlAsync(avatarUrl, minioService.GetImagesBucket(), 300);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to generate presigned URL for creator avatar {AvatarKey}", avatarUrl);
                    avatarUrl = null;
                }
            }

            authorsPreviews.Add(new AuthorPreviewDto
            {
                AvatarUrl = avatarUrl ?? string.Empty,
                Id = page.UserId,
                PageName = page.PageName
            });
        }

        return authorsPreviews;
    }
}
