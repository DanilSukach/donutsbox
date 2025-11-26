using Auth.Api.Dto;
using Donutsbox.Domain.Repositories.AuthRepository;
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
            throw new InvalidOperationException("Неверный текущий пароль");
        }
        
        if (dto.NewPassword != dto.RepeatNewPassword)
        {
            logger.LogWarning("New passwords do not match for user {UserId}", userId);
            throw new InvalidOperationException("Новые пароли не совпадают");
        }
        
        if (dto.OldPassword == dto.NewPassword)
        {
            logger.LogWarning("New password is the same as old password for user {UserId}", userId);
            throw new InvalidOperationException("Новый пароль должен отличаться от старого");
        }

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
            throw new InvalidOperationException("Этот email уже используется");
        }
        userEntity.AuthEmail = dto.Email;
        await repository.UpdateAsync(userEntity);
        return true;
    }
}
