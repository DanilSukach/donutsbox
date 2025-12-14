namespace Donutsbox.Api.Dto;

public class UpdateAudienceRequestDto
{
    public bool? IsPublic { get; set; }
    public List<Guid>? SubscriptionIds { get; set; }
}

