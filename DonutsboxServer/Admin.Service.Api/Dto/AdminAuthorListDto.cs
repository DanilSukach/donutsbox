namespace Admin.Service.Api.Dto;

public class AdminAuthorListDto
{
    public Guid Id { get; set; }
    public Guid CreatorPageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int PostsCount { get; set; }
    public int SubscriptionsCount { get; set; }
    public int SubscribersCount { get; set; }
    public bool IsShadowBanned { get; set; }
}
