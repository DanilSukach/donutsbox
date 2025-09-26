namespace Auth.Api.Dto;

public class RegisterRequestDto
{
    /// <summary>
    /// Email для регистрации
    /// </summary>
    public required string AuthEmail { get; set; }
    /// <summary>
    /// пассворд
    /// </summary>
    public required string Password { get; set; }
    /// <summary>
    /// Повторный пароль
    /// </summary>
    public required string RepeatPassword { get; set; }
}
