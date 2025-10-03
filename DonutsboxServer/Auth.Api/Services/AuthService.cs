using Auth.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthRepository;

namespace Auth.Api.Services;

public class AuthService(IAuthRepository repository, IJwtService jwt) : IAuthService
{
    public async Task RegisterAsync(RegisterRequestDto dto)
    {
        if (dto.Password != dto.RepeatPassword)
        {
            throw new InvalidOperationException("Password doesn't match");
        }

        if (dto.Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Administrator role cannot be created through registration");
        }

        if (await repository.EmailExistsAsync(dto.AuthEmail))
        {
            throw new InvalidOperationException("Email exists");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new UserAuth
        {
            Id = Guid.NewGuid(),
            Password = hash,
            AuthEmail = dto.AuthEmail,
        };

        await repository.AddAsync(user, dto.Role);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await repository.GetByEmailAsync(dto.EmailAuth);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        string accessToken;
        if (user.LastAuth == null && user.User!.UserType.Name == "Creator")
        {
            accessToken = jwt.GenerateAccessToken(user, true);
        }
        else
        {
            accessToken = jwt.GenerateAccessToken(user, false);
        }
        var refreshToken = jwt.GenerateRefreshToken();

        user.LastAuth = DateTime.UtcNow;
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
        var user = await repository.GetByEmailAsync(dto.RefreshToken) ?? throw new UnauthorizedAccessException("Invalid refresh token");
        var newAccessToken = jwt.GenerateAccessToken(user, false);
        var newRefreshToken = jwt.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        user.LastAuth = DateTime.UtcNow;

        await repository.UpdateAsync(user);

        var tokens = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };

        return tokens;
    }

    public async Task CreateAdminAsync(RegisterRequestDto dto)
    {
        if (dto.Password != dto.RepeatPassword)
        {
            throw new InvalidOperationException("Password doesn't match");
        }

        var emailExists = await repository.EmailExistsAsync(dto.AuthEmail);
        if (emailExists)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new UserAuth
        {
            Id = Guid.NewGuid(),
            Password = hash,
            AuthEmail = dto.AuthEmail,
        };

        await repository.AddAsync(user, "Administrator");
    }
}
