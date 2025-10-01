using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;
using System.Security.Claims;

namespace Donutsbox.Api.Services.UserInteractionService;

public class UserInteractionService(IEntityRepository<UserSubscription, Guid> userSubscriptionRepository, IEntityRepository<User, Guid> userRepository, IEntityRepository<Subscription, Guid> subscriptionRepository) : IUserInteractionService
{
    public async Task<UserSubscriptionDto> SubscribeUserAsync(UserSubscriptionCreateDto userSubscription, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await userRepository.GetByIdAsync(userId);
        var subscription = await subscriptionRepository.GetByIdAsync(userSubscription.SubscriptionId);

        var userSubscriptionEntity = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionId = userSubscription.SubscriptionId,
            User = userEntity!,
            Subscription = subscription!,
            BeginDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(subscription!.SubscriptionPeriod.Months)
        };
        var result = await userSubscriptionRepository.AddAsync(userSubscriptionEntity);
        return new UserSubscriptionDto
        {
            Id = result.Id,
            UserId = result.UserId,
            SubscriptionId = result.SubscriptionId,
            BeginDate = result.BeginDate,
            EndDate = result.EndDate
        };
    }
}
