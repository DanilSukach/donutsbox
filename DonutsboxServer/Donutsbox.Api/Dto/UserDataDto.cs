namespace Donutsbox.Api.Dto;

public class UserDataDto
{
    /// <summary>
    /// Id непоссредственного пользователя
    /// </summary>
    public required Guid UserId { get; set; }
    /// <summary>
    /// Ссылка на хранилище аватарки
    /// </summary>
    public required string AvatarUrl { get; set; }
    /// <summary>
    /// Раздел "о себе" пользователя
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Email для уведомлений
    /// </summary>
    public string? NotificationEmail { get; set; }
    /// <summary>
    /// Номер телефона
    /// </summary>
    public string? PhoneNumber { get; set; }
    /// <summary>
    /// TBD
    /// </summary>
    public string? PaymentInfo { get; set; }
}
