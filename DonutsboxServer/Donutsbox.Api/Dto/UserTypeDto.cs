namespace Donutsbox.Api.Dto;

public class UserTypeDto
{
    /// <summary>
    /// Id типа пользователя
    /// </summary>
    public required int Id { get; set; }
    /// <summary>
    /// Имя типа
    /// </summary>
    public required string Name { get; set; }
}