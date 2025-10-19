using Donutsbox.Api.Dto;
using Donutsbox.Domain.Repositories.UserSubscriptionsRepository;
using System.Security.Claims;

namespace Donutsbox.Api.Services.UserSubscriptionsService;

public class UserSubscriptionsService(IUserSubscriptionsRepository userRepository) : IUserSubscriptionsService
{
    public async Task<IEnumerable<AuthorPreviewDto>> GetAuthorPagesFromUserSubscribes(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await userRepository.GetByIdUserWithSubscriptionsAsync(userId);
        var authorPages = userEntity!.UserSubscriptions.Select(us => us.Subscription).Select(s => s.CreatorPageData).ToList();
        var authorsPreviews = new List<AuthorPreviewDto>();
        foreach (var page in authorPages)
        {
            authorsPreviews.Add(new AuthorPreviewDto
            {
                AvatarUrl = page.AvatarURL!,
                Id = page.UserId,
                PageName = page.PageName
            });
        }
        return authorsPreviews;
    }
}
