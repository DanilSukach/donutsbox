namespace Donutsbox.Api.Dto;

/// <summary>
/// Информация о пользователе
/// </summary>
public class UserRequestDto
{
    /// <summary>
    /// Id
    /// </summary>
    public required Guid Id { get; set; }
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public required string UserName { get; set; }
}
