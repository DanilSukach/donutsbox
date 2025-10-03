using Donutsbox.Domain.Entities;
using System.Security.Claims;

namespace Auth.Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(UserAuth user, bool isNewCreator);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateAccessToken(string token);
    Guid? GetUserIdFromToken(string token);
    string? GetUsernameFromToken(string token);
}
