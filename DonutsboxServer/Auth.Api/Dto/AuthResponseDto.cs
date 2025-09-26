namespace Auth.Api.Dto;

/// <summary>
/// Класс для отправки токенов
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// Access токен
    /// </summary>
    public required string AccessToken { get; set; }
    /// <summary>
    /// Refresh токен
    /// </summary>
    public required string RefreshToken { get; set; }
}
