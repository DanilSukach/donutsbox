using Donutsbox.Api.Dto;
using Donutsbox.Api.Hubs;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;
using Microsoft.AspNetCore.SignalR;

namespace Donutsbox.Api.Services.PostCommentService;

public class PostCommentService(
    IEntityRepository<PostComment, Guid> comment, 
    IEntityRepository<ContentPost, Guid> post, 
    IEntityRepository<User, Guid> user, 
    IHubContext<CommentsHub> hubContext) : IPostCommentService
{
    public async Task<PostCommentDto?> AddAsync(CreatePostCommentDto dto, Guid userId)
    {
        var userEntity = await user.GetByIdAsync(userId) ?? throw new InvalidOperationException($"User with ID {userId} not found");

        var postEntity = await post.GetByIdAsync(dto.PostId) ?? throw new InvalidOperationException($"Post with ID {dto.PostId} not found");

        var commentEntity = new PostComment
        {
            Id = Guid.NewGuid(),
            ContentPostId = dto.PostId,
            ContentPost = postEntity,
            UserId = userId,
            User = userEntity,
            Text = dto.Text,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var addedComment = await comment.AddAsync(commentEntity);

        var result = new PostCommentDto
        {
            Id = addedComment.Id,
            PostId = addedComment.ContentPostId,
            UserId = addedComment.UserId,
            UserName = userEntity.Name,
            Text = addedComment.Text,
            CreatedAt = addedComment.CreatedAt
        };

        await hubContext.Clients
            .Group($"post-{dto.PostId}")
            .SendAsync("CommentAdded", result);

        return result;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var commentEntity = await comment.GetByIdAsync(id) ?? throw new InvalidOperationException($"Comment with ID {id} not found");

        if (commentEntity.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this comment");

        var postId = commentEntity.ContentPostId;

        await comment.DeleteAsync(id);

        await hubContext.Clients
            .Group($"post-{postId}")
            .SendAsync("CommentDeleted", id);
    }

    public async Task UpdateAsync(Guid id, string text, Guid userId)
    {
        var commentEntity = await comment.GetByIdAsync(id) ?? throw new InvalidOperationException($"Comment with ID {id} not found");

        if (commentEntity.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this comment");

        commentEntity.Text = text;
        await comment.UpdateAsync(commentEntity, id);

        await hubContext.Clients
            .Group($"post-{commentEntity.ContentPostId}")
            .SendAsync("CommentUpdated", new { id = commentEntity.Id, text });
    }

    public async Task<IEnumerable<PostCommentDto>> GetByPostIdAsync(Guid postId)
    {
        var postEntity = await post.GetByIdAsync(postId) ?? throw new InvalidOperationException($"Post with ID {postId} not found");

        return postEntity.PostComments.Select(c => new PostCommentDto()
        {
            Id = c.Id,
            Text = c.Text,
            UserId = c.UserId,
            PostId = c.ContentPostId,
            CreatedAt = c.CreatedAt,
            UserName = c.User.Name
        });

    }
}
