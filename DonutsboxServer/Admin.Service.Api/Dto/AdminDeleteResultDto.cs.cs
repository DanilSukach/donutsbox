namespace Admin.Service.Api.Dto;

/// <summary>
/// DTO для результата удаления
/// </summary>
public class AdminDeleteResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> DeletedEntities { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
