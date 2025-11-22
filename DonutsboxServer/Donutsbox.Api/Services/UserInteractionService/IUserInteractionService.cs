using Donutsbox.Api.Dto;
using System.Security.Claims;

namespace Donutsbox.Api.Services.UserInteractionService;

public interface IUserInteractionService
{
    Task UnsubscribeUserAsync(Guid creatorUserId, ClaimsPrincipal user);
    Task<bool> ChangeReactionAsync(ClaimsPrincipal user, ContentPostReactionDto reaction);
}
