using Auth.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthRepository;
using Donutsbox.Domain.Repositories.EntityRepository;
using System.Security.Claims;

namespace Auth.Api.Services;

public class UserProfileService(IAuthRepository repository) : IUserProfileService
{
    public async Task<bool> ChangePassword(NewPasswordDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await repository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        if (userEntity == null || !BCrypt.Net.BCrypt.Verify(dto.OldPassword, userEntity.Password))
            throw new UnauthorizedAccessException("Invalid credentials");
        if (dto.NewPassword != dto.RepeatNewPassword)
            throw new InvalidOperationException("New passwords do not match");
        if (dto.OldPassword == dto.NewPassword)
            throw new InvalidOperationException("New password must be different from the old password");

        await repository.UpdateAsync(new UserAuth
        {
            Id = userEntity.Id,
            AuthEmail = userEntity.AuthEmail,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword),
            User = userEntity.User,
            LastAuth = userEntity.LastAuth,
            RefreshToken = userEntity.RefreshToken,
            RefreshTokenExpiryTime = userEntity.RefreshTokenExpiryTime
        });
        return true;
    }

    public async Task<bool> ChangeEmail(NewEmailDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await repository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");
        if (await repository.EmailExistsAsync(dto.Email))
        {
            throw new InvalidOperationException("Email exists");
        }
        await repository.UpdateAsync(new UserAuth
        {
            Id = userEntity.Id,
            AuthEmail = dto.Email,
            Password = userEntity.Password,
            User = userEntity.User,
            LastAuth = userEntity.LastAuth,
            RefreshToken = userEntity.RefreshToken,
            RefreshTokenExpiryTime = userEntity.RefreshTokenExpiryTime
        });
        return true;
    }
}
