using Auth.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthRepository;
using Donutsbox.Domain.Repositories.EntityRepository;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Auth.Api.Services;

public class UserProfileService(IAuthRepository repository, ILogger<UserProfileService> logger) : IUserProfileService
{
    public async Task<bool> ChangePassword(NewPasswordDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        
        logger.LogInformation("Attempting to change password for user {UserId}", userId);
        
        var userEntity = await repository.GetByUserIdAsync(userId) ?? throw new InvalidOperationException("User not found");
        
        logger.LogInformation("User found: {Email}", userEntity.AuthEmail);
        
        logger.LogInformation("User found, verifying old password");

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, userEntity.Password))
        {
            logger.LogWarning("Invalid old password for user {UserId}", userId);
            throw new UnauthorizedAccessException("Invalid old password");
        }
        
        if (dto.NewPassword != dto.RepeatNewPassword)
        {
            logger.LogWarning("New passwords do not match for user {UserId}", userId);
            throw new InvalidOperationException("New passwords do not match");
        }
        
        if (dto.OldPassword == dto.NewPassword)
        {
            logger.LogWarning("New password is the same as old password for user {UserId}", userId);
            throw new InvalidOperationException("New password must be different from the old password");
        }

        // Обновляем только пароль существующей сущности
        userEntity.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        
        logger.LogInformation("Updating password for user {UserId}", userId);
        await repository.UpdateAsync(userEntity);
        
        logger.LogInformation("Password successfully updated for user {UserId}", userId);
        return true;
    }

    public async Task<bool> ChangeEmail(NewEmailDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await repository.GetByUserIdAsync(userId) ?? throw new InvalidOperationException("User not found");
        if (await repository.EmailExistsAsync(dto.Email))
        {
            throw new InvalidOperationException("Email exists");
        }
        // Обновляем только email существующей сущности
        userEntity.AuthEmail = dto.Email;
        await repository.UpdateAsync(userEntity);
        return true;
    }
}
