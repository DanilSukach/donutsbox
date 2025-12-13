using System;
using System.Text.RegularExpressions;
using Auth.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthRepository;

namespace Auth.Api.Services;

public class AuthService(IAuthRepository repository, IJwtService jwt) : IAuthService
{
    public async Task RegisterAsync(RegisterRequestDto dto)
    {
        // Валидация email
        if (string.IsNullOrWhiteSpace(dto.AuthEmail))
        {
            throw new InvalidOperationException("Email is required");
        }

        var email = dto.AuthEmail.Trim().ToLowerInvariant();
        
        // Проверка формата email
        if (!IsValidEmail(email))
        {
            throw new InvalidOperationException("Invalid email format");
        }

        // Валидация пароля
        var (IsValid, ErrorMessage) = ValidatePassword(dto.Password);
        if (!IsValid)
        {
            throw new InvalidOperationException(ErrorMessage);
        }

        if (dto.Password != dto.RepeatPassword)
        {
            throw new InvalidOperationException("Password doesn't match");
        }

        if (dto.Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Administrator role cannot be created through registration");
        }

        if (await repository.EmailExistsAsync(email))
        {
            throw new InvalidOperationException("Email exists");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new UserAuth
        {
            Id = Guid.NewGuid(),
            Password = hash,
            AuthEmail = email,
        };

        await repository.AddAsync(user, dto.Role);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
        {
            return false;
        }

        // Базовый regex для проверки формата email
        if (!EmailRegex.IsMatch(email))
        {
            return false;
        }

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            return false;
        }

        var localPart = parts[0];
        var domain = parts[1];

        // Проверка локальной части
        if (localPart.Length == 0 || localPart.Length > 64)
        {
            return false;
        }

        // Проверка домена
        if (domain.Length == 0 || domain.Length > 253)
        {
            return false;
        }

        // Проверка, что домен содержит точку
        if (!domain.Contains('.'))
        {
            return false;
        }

        // Проверка, что домен не начинается и не заканчивается точкой или дефисом
        if (domain.StartsWith('.') || domain.EndsWith('.') ||
            domain.StartsWith('-') || domain.EndsWith('-'))
        {
            return false;
        }

        return true;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await repository.GetByEmailAsync(dto.EmailAuth);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var isCreator = string.Equals(user.User!.UserType.Name, "Creator", StringComparison.OrdinalIgnoreCase);
        var isNewCreator = isCreator && user.LastAuth == null;
        var isFirstLogin = user.LastAuth == null || user.User.Name == "User";
        string accessToken = jwt.GenerateAccessToken(user, isNewCreator);
        var refreshToken = jwt.GenerateRefreshToken();

        if (!isFirstLogin)
        {
            user.LastAuth = DateTime.UtcNow;
        }
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await repository.UpdateAsync(user);

        var tokens = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.User.Id,
            Role = user.User.UserType.Name,
            IsCreator = isCreator,
            HasCreatorPage = user.User.CreatorPageData != null,
            CreatorPageId = user.User.CreatorPageData?.Id,
            IsNewCreator = isNewCreator,
            IsFirstLogin = isFirstLogin
        };

        return tokens;
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshRequestDto dto)
    {
        var user = await repository.GetByRefreshTokenAsync(dto.RefreshToken) ?? throw new UnauthorizedAccessException("Invalid refresh token");
        var newAccessToken = jwt.GenerateAccessToken(user, false);
        var newRefreshToken = jwt.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        user.LastAuth = DateTime.UtcNow;

        await repository.UpdateAsync(user);

        var tokens = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            UserId = user.User!.Id,
            Role = user.User!.UserType.Name,
            IsCreator = string.Equals(user.User.UserType.Name, "Creator", StringComparison.OrdinalIgnoreCase),
            HasCreatorPage = user.User.CreatorPageData != null,
            CreatorPageId = user.User.CreatorPageData?.Id,
            IsNewCreator = false
        };

        return tokens;
    }

    public async Task CreateAdminAsync(RegisterRequestDto dto)
    {
        // Валидация email
        if (string.IsNullOrWhiteSpace(dto.AuthEmail))
        {
            throw new InvalidOperationException("Email is required");
        }

        var email = dto.AuthEmail.Trim().ToLowerInvariant();
        
        // Проверка формата email
        if (!IsValidEmail(email))
        {
            throw new InvalidOperationException("Invalid email format");
        }

        // Валидация пароля
        var (IsValid, ErrorMessage) = ValidatePassword(dto.Password);
        if (!IsValid)
        {
            throw new InvalidOperationException(ErrorMessage);
        }

        if (dto.Password != dto.RepeatPassword)
        {
            throw new InvalidOperationException("Password doesn't match");
        }

        var emailExists = await repository.EmailExistsAsync(email);
        if (emailExists)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new UserAuth
        {
            Id = Guid.NewGuid(),
            Password = hash,
            AuthEmail = email,
        };

        await repository.AddAsync(user, "Administrator");
    }

    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

    /// <summary>
    /// Валидирует пароль согласно политике безопасности.
    /// </summary>
    /// <param name="password">Пароль для валидации.</param>
    /// <returns>Результат валидации с сообщением об ошибке, если пароль не соответствует требованиям.</returns>
    private static (bool IsValid, string ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required");
        }

        // Минимум 8 символов
        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        // Максимум 128 символов (разумный лимит)
        if (password.Length > 128)
        {
            return (false, "Password must not exceed 128 characters");
        }

        // Проверка: не только цифры
        if (password.All(char.IsDigit))
        {
            return (false, "Password cannot consist only of digits");
        }

        // Проверка: не только буквы
        if (password.All(char.IsLetter))
        {
            return (false, "Password must contain at least one digit or special character");
        }

        // Проверка: есть заглавные буквы
        if (!password.Any(char.IsUpper))
        {
            return (false, "Password must contain at least one uppercase letter");
        }

        // Проверка: есть строчные буквы
        if (!password.Any(char.IsLower))
        {
            return (false, "Password must contain at least one lowercase letter");
        }

        // Проверка: есть цифры
        if (!password.Any(char.IsDigit))
        {
            return (false, "Password must contain at least one digit");
        }

        // Проверка: есть специальные символы
        var specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        if (!password.Any(c => specialChars.Contains(c)))
        {
            return (false, "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)");
        }

        return (true, string.Empty);
    }
}
