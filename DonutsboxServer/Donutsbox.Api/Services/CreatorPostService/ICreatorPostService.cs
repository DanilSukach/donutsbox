using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using System.Security.Claims;

namespace Donutsbox.Api.Services.CreatorPostService;

public interface ICreatorPostService
{
    Task<PostDraftResponseDto> CreateDraftAsync(CreateDraftRequestDto request, ClaimsPrincipal user);
    Task<AddVideosResponseDto> AddVideosToPostAsync(Guid postId, AddVideosRequestDto request, ClaimsPrincipal user);
    Task<AddImagesResponseDto> AddImagesToPostAsync(Guid postId, AddImagesRequestDto request, ClaimsPrincipal user);
    Task<AddTextResponseDto> AddTextToPostAsync(Guid postId, AddTextRequestDto request, ClaimsPrincipal user);
    Task<PublishPostResponseDto> PublishPostAsync(Guid postId, ClaimsPrincipal user);
    Task<MessageResponseDto> UnpublishPostAsync(Guid postId, ClaimsPrincipal user);
    Task<UploadImagesResponseDto> UploadImagesAsync(UploadImagesRequestDto request, ClaimsPrincipal user);
    Task<MyPostsResponseDto> GetMyPostsAsync(int page, int pageSize, bool? isPublished, ClaimsPrincipal user);
    Task<CreatorPostsResponseDto> GetCreatorPublicPostsAsync(Guid creatorId, ClaimsPrincipal user, int page, int pageSize);
    Task<byte[]> GetImageAsync(string imagePath);
    Task<MessageResponseDto> DeletePostAsync(Guid postId, ClaimsPrincipal user);
    Task<MyPostsResponseDto> GetSubscriptionFeedAsync(int page, int pageSize, ClaimsPrincipal user);
    Task<MessageResponseDto> CancelVideoProcessingAsync(Guid videoId, ClaimsPrincipal user);
    Task<MessageResponseDto> DeleteVideoAsync(Guid videoId, ClaimsPrincipal user);
    Task<MessageResponseDto> DeleteImageAsync(string imageKey, ClaimsPrincipal user);
    Task<bool> TryPublishPostAfterMediaProcessingAsync(Guid postId);
}
