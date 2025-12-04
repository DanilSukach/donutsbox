using System.Security.Claims;
using Donutsbox.Api.Dto;

namespace Donutsbox.Api.Services;

public interface IUserService : IEntityService<UserDto, Guid>
{
    Task<bool> ChangeUserName(UserNameDto dto, ClaimsPrincipal user);
    Task<bool> CompleteFirstLogin(FirstLoginDto dto, ClaimsPrincipal user);
    Task<bool> SkipFirstLogin(ClaimsPrincipal user);
}

