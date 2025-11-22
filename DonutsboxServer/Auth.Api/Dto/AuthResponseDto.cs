using System;

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

    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsCreator { get; set; }
    public Guid? CreatorPageId { get; set; }
    public bool HasCreatorPage { get; set; }
    public bool IsNewCreator { get; set; }
}
