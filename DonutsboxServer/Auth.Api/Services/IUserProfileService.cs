using Auth.Api.Dto;
using System.Security.Claims;

namespace Auth.Api.Services;

public interface IUserProfileService
{
    Task<bool> ChangePassword(NewPasswordDto dto, ClaimsPrincipal user);
    Task<bool> ChangeEmail(NewEmailDto dto, ClaimsPrincipal user);
}
