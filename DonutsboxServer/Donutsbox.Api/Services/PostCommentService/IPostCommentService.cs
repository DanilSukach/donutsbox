using Donutsbox.Api.Dto;

namespace Donutsbox.Api.Services.PostCommentService;

public interface IPostCommentService
{
    Task<PostCommentDto?> AddAsync(CreatePostCommentDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task UpdateAsync(Guid id, string text, Guid userId);
    Task<IEnumerable<PostCommentDto>> GetByPostIdAsync(Guid postId);
}
