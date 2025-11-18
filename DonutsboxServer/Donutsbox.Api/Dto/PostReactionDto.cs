namespace Donutsbox.Api.Dto;

public class PostReactionDto
{
    public required Guid Id { get; set; }
    public required Guid ContentPostId { get; set; }
    public required Guid UserId { get; set; }
    public required int ReactionTypeId { get; set; }
}
