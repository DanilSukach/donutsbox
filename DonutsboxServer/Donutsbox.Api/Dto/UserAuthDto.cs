namespace Donutsbox.Api.Dto;

public class UserAuthDto
{
    /// <summary>
    /// Email для аунтентификации
    /// </summary>
    public required string AuthEmail { get; set; }
    /// <summary>
    /// пассворд
    /// </summary>
    public required string Password { get; set; }
    /// <summary>
    /// Дата последнего входа
    /// </summary>
    public required DateTime LastAuth { get; set; }
}
