using Donutsbox.Api.Dto;
using System.Security.Claims;

namespace Donutsbox.Api.Services.AuthorService;

public interface IAuthorService
{
    Task<IEnumerable<AuthorRequestDto>> GetAuthorsAsync(int page, int pageSize, string? sortBy = null, bool descending = false);
    Task<AuthorRequestDto?> GetAuthorByIdAsync(Guid id);
    Task<IEnumerable<AuthorRequestDto>> GetTopAuthorsAsync(int count);
    Task<CreatorPageDataDto> AddCreatorPageAsync(CreatorPageDataDto dto, ClaimsPrincipal user);
}
