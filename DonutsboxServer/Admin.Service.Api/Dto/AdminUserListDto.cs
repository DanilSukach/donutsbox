namespace Admin.Service.Api.Dto;

/// <summary>
/// DTO для отображения пользователя в админ-панели
/// </summary>
public class AdminUserListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool HasCreatorPage { get; set; }
    public int PostsCount { get; set; }
    public int SubscriptionsCount { get; set; }
}
