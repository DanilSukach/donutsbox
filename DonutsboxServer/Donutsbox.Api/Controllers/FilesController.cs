using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.FilesService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FilesController(IFilesService filesService) : ControllerBase
{
    /// <summary>
    /// Загружает видео (только для creator)
    /// </summary>
    [Authorize(Roles = "Creator")]
    [HttpPost("upload")]
    [RequestSizeLimit(10L * 1024 * 1024 * 1024)]
    public async Task<ActionResult<VideoUploadResponseDto>> Upload([FromForm] VideoUploadRequestDto request)
    {
        return await ExecuteAsync(() => filesService.UploadVideoAsync(GetUserId(), request));
    }

    /// <summary>
    /// Получить список видео текущего creator'а
    /// </summary>
    [Authorize(Roles = "Creator")]
    [HttpGet("my-videos")]
    public async Task<ActionResult<MyVideoResponseDto>> GetMyVideos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        return await ExecuteAsync(() => filesService.GetMyVideosAsync(GetUserId(), page, pageSize, status));
    }

    /// <summary>
    /// Получить превью видео
    /// </summary>
    [Authorize]
    [HttpGet("{videoId:guid}/thumbnail")]
    public async Task<ActionResult<byte[]>> GetThumbnail([FromRoute] Guid videoId)
    {
        return await ExecuteFileAsync(() => filesService.GetThumbnailAsync(videoId));
    }

    /// <summary>
    /// HLS манифест
    /// </summary>
    [Authorize]
    [HttpGet("{videoId:guid}/hls/index.m3u8")]
    public async Task<ActionResult<byte[]>> GetManifest([FromRoute] Guid videoId)
    {
        return await ExecuteFileAsync(() => filesService.GetManifestAsync(videoId));
    }

    [Authorize]
    [HttpGet("{videoId:guid}/hls/{segment}")]
    public async Task<ActionResult<byte[]>> GetSegment([FromRoute] Guid videoId, [FromRoute] string segment)
    {
        return await ExecuteFileAsync(() => filesService.GetSegmentAsync(videoId, segment));
    }

    [Authorize]
    [HttpPost("images/avatar")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ImageUploadResponseDto>> UploadAvatar([FromForm] ImageUploadRequestDto request)
    {
        return await ExecuteAsync(() => filesService.UploadAvatarAsync(GetUserId(), request));
    }

    [Authorize]
    [HttpPost("images/banner")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImageUploadResponseDto>> UploadBanner([FromForm] ImageUploadRequestDto request)
    {
        return await ExecuteAsync(() => filesService.UploadBannerAsync(GetUserId(), request));
    }

    [Authorize]
    [HttpGet("images/url")]
    public async Task<ActionResult<ImageUrlResponseDto>> GetImageUrl([FromQuery] string key, [FromQuery] int ttl = 300)
    {
        return await ExecuteAsync(() => filesService.GetImageUrlAsync(key, ttl));
    }

    [Authorize(Roles = "Creator")]
    [HttpPost("images/post")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    public async Task<ActionResult<List<ImageUploadResponseDto>>> UploadPostImage([FromForm] UploadPostImageDto dto)
    {
        return await ExecuteAsync(() => filesService.UploadPostImagesAsync(GetUserId(), dto));
    }

    /// <summary>
    /// Загружает аудио (только для creator)
    /// </summary>
    [Authorize(Roles = "Creator")]
    [HttpPost("audio")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    public async Task<ActionResult<AudioUploadResponseDto>> UploadAudio([FromForm] AudioUploadRequestDto request)
    {
        return await ExecuteAsync(() => filesService.UploadAudioAsync(GetUserId(), request));
    }

    /// <summary>
    /// Получить presigned URL для прослушивания аудио
    /// </summary>
    [Authorize]
    [HttpGet("audio/url")]
    public async Task<ActionResult<AudioUrlResponseDto>> GetAudioUrl([FromQuery] string key, [FromQuery] int ttl = 300)
    {
        return await ExecuteAsync(() => filesService.GetAudioUrlAsync(key, ttl));
    }

    /// <summary>
    /// Удалить аудио (только для creator)
    /// </summary>
    [Authorize(Roles = "Creator")]
    [HttpDelete("audio/{audioId:guid}")]
    public async Task<ActionResult<MessageResponseDto>> DeleteAudio([FromRoute] Guid audioId)
    {
        return await ExecuteAsync(() => filesService.DeleteAudioAsync(audioId, GetUserId()));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!.Value);
    }

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return Ok(result);
        }
        catch (FilesServiceException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    private async Task<ActionResult<byte[]>> ExecuteFileAsync(Func<Task<FilePayload>> action)
    {
        try
        {
            var payload = await action();
            return File(payload.Bytes, payload.ContentType);
        }
        catch (FilesServiceException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }
}
