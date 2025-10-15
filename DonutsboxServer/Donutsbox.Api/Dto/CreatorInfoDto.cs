namespace Donutsbox.Api.Dto;

public class CreatorInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Description { get; set; }
    public int SubscribersCount { get; set; }
}
