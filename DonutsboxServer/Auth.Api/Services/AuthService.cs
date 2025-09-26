using Auth.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories;

namespace Auth.Api.Services;

public class AuthService(IAuthRepository repository, IJwtService jwt) : IAuthService
{
    public async Task RegisterAsync(RegisterRequestDto dto)
    {
        if (dto.Password != dto.RepeatPassword)
        {
            throw new InvalidOperationException("Password doesn't match");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new UserAuth
        {
            Id = Guid.NewGuid(),
            Password = hash,
            AuthEmail = dto.AuthEmail
        };

        await repository.AddAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await repository.GetByEmailAsync(dto.EmailAuth);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await repository.UpdateAsync(user);

        var tokens = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        return tokens;
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshRequestDto dto)
    {
        var user = await repository.GetByRefreshTokenAsync(dto.RefreshToken) ?? throw new UnauthorizedAccessException("Invalid refresh token");
        var newAccessToken = jwt.GenerateAccessToken(user);
        var newRefreshToken = jwt.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await repository.UpdateAsync(user);

        var tokens = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };

        return tokens;
    }


}
