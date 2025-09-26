namespace Auth.Api.Dto;

/// <summary>
/// Класс аутентификации пользователя
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public required string EmailAuth { get; set; }
    /// <summary>
    /// Пароль
    /// </summary>
    public required string Password { get; set; }
}
