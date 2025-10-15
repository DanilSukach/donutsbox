using Donutsbox.Api.Dto;
using System.Security.Claims;

namespace Donutsbox.Api.Services.UserSubscriptionsService;

public interface IUserSubscriptionsService
{
    Task<IEnumerable<AuthorPreviewDto>> GetAuthorPagesFromUserSubscribes(ClaimsPrincipal user);
}
