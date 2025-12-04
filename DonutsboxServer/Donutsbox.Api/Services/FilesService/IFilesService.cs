using Donutsbox.Api.Dto;

namespace Donutsbox.Api.Services.FilesService;

public interface IFilesService
{
    Task<VideoUploadResponseDto> UploadVideoAsync(Guid userId, VideoUploadRequestDto request);
    Task<MyVideoResponseDto> GetMyVideosAsync(Guid userId, int page, int pageSize, string? status);
    Task<FilePayload> GetThumbnailAsync(Guid videoId);
    Task<FilePayload> GetManifestAsync(Guid videoId);
    Task<FilePayload> GetSegmentAsync(Guid videoId, string segment);
    Task<ImageUploadResponseDto> UploadAvatarAsync(Guid userId, ImageUploadRequestDto request);
    Task<ImageUploadResponseDto> UploadBannerAsync(Guid userId, ImageUploadRequestDto request);
    Task<ImageUrlResponseDto> GetImageUrlAsync(string key, int ttl);
    Task<List<ImageUploadResponseDto>> UploadPostImagesAsync(Guid userId, UploadPostImageDto dto);
    Task<AudioUploadResponseDto> UploadAudioAsync(Guid userId, AudioUploadRequestDto request);
    Task<AudioUrlResponseDto> GetAudioUrlAsync(string key, int ttl);
    Task<MessageResponseDto> DeleteAudioAsync(Guid audioId, Guid userId);
}

