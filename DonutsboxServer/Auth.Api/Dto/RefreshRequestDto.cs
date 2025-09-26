namespace Auth.Api.Dto;

/// <summary>
/// Класс для refresh токена
/// </summary>
public class RefreshRequestDto
{
    /// <summary>
    /// Refresh токен
    /// </summary>
    public required string RefreshToken { get; set; }
}
