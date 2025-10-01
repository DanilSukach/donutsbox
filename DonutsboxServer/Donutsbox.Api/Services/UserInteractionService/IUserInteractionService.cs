using Donutsbox.Api.Dto;
using System.Security.Claims;

namespace Donutsbox.Api.Services.UserInteractionService;

public interface IUserInteractionService
{
    Task<UserSubscriptionDto> SubscribeUserAsync(UserSubscriptionCreateDto userSubscription, ClaimsPrincipal user);
}
