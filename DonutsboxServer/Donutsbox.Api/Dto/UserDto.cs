namespace Donutsbox.Api.Dto;

public class UserDto
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public required Guid GUID { get; set; }
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Тип пользователя
    /// </summary>
    public required int TypeId { get; set; }
    /// <summary>
    /// Сущность для авторизации
    /// </summary>
    public required string AuthId { get; set; }
}