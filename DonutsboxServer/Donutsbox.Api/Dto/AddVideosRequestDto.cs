namespace Donutsbox.Api.Dto;

public class AddVideosRequestDto
{
    public List<Guid> VideoIds { get; set; } = [];
    public bool? IsPublic { get; set; }
    public List<Guid>? SubscriptionIds { get; set; }
}
