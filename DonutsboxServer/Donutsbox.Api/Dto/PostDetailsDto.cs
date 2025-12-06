namespace Donutsbox.Api.Dto;

public class PostDetailsDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsPublished { get; set; }
    public int LikesCount { get; set; }
    public int DislikesCount { get; set; }
    public int CommentsCount { get; set; }
    public List<PostVideoDto> Videos { get; set; } = [];
    public List<PostAudioDto> Audios { get; set; } = [];
    public List<string> PictureUrls { get; set; } = [];
    public string? CreatorPageName { get; set; }
    public Guid? CreatorId { get; set; }
    public string? CreatorAvatarUrl { get; set; }
    public int ReactionTypeId { get; set; }
    public string AudienceType { get; set; } = "Public";
    public List<Guid> SubscriptionIds { get; set; } = [];
    public bool IsLocked { get; set; }
    public string? LockedMessage { get; set; }
    /// <summary>
    /// Находится ли пост в теневом бане
    /// </summary>
    public bool IsShadowBanned { get; set; } = false;
}