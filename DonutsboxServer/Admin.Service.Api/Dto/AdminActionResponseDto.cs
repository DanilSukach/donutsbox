namespace Admin.Service.Api.Dto;

/// <summary>
/// DTO для ответа на действия администратора
/// </summary>
public class AdminActionResponseDto
{
    /// <summary>
    /// Успешность операции
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Сообщение о результате операции
    /// </summary>
    public string? Message { get; set; }
}

