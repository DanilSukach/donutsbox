using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;
using System.Security.Claims;

namespace Donutsbox.Api.Services.UserSubscriptionsService;

public class UserSubscriptionsService(IEntityRepository<User, Guid> userRepository, IEntityRepository<CreatorPageData, Guid> creatorPageDataRepository)
{
    public async Task<IEnumerable<AuthorPreviewDto>> GetUserSubscribes(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await userRepository.GetByIdAsync(userId);
        userEntity.UserSubscriptions
    }
}
