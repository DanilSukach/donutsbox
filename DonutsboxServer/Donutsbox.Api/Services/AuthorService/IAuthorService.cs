using Donutsbox.Api.Dto;
using System.Security.Claims;

namespace Donutsbox.Api.Services.AuthorService;

public interface IAuthorService
{
    Task<IEnumerable<AuthorRequestDto>> GetAuthorsAsync(int page, int pageSize, string? sortBy = null, bool descending = false);
    Task<IEnumerable<AuthorRequestDto>> GetAuthorsAsync();
    Task<AuthorRequestDto?> GetAuthorByIdAsync(Guid id);
    Task<IEnumerable<AuthorRequestDto>> GetTopAuthorsAsync(int count);
    Task<IEnumerable<UserRequestDto>> GetTopSupportedUsersAsync(ClaimsPrincipal author, int count);
    Task<CreatorPageDataDto> AddCreatorPageAsync(CreatorPageDataDto dto, ClaimsPrincipal user);
    Task<SubscriptionDto> AddSubscriptionAsync(SubscriptionCreateDto dto, ClaimsPrincipal user);
    Task<bool> UpdateBannerAsync(string bannerKey, ClaimsPrincipal user);
}
