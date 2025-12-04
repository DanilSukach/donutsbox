namespace Donutsbox.Api.Dto;

/// <summary>
/// DTO для заполнения данных при первом входе
/// </summary>
public class FirstLoginDto
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Номер телефона
    /// </summary>
    public string? PhoneNumber { get; set; }
}

