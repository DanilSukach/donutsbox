using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.CreatorPostService;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CreatorPostController(ICreatorPostService creatorPostService) : ControllerBase
{
    /// <summary>
    /// Шаг 1: Создать черновик поста (не опубликован)
    /// </summary>
    [HttpPost("draft")]
    public async Task<ActionResult<PostDraftResponseDto>> CreateDraft([FromBody] CreateDraftRequestDto request)
    {
        try
        {
            var result = await creatorPostService.CreateDraftAsync(request, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Шаг 2: Добавить видео к черновику поста
    /// </summary>
    [HttpPost("{postId:guid}/videos")]
    public async Task<ActionResult<AddVideosResponseDto>> AddVideosToPost(
      [FromRoute] Guid postId,
      [FromBody] AddVideosRequestDto request)
    {
        try
        {
            var result = await creatorPostService.AddVideosToPostAsync(postId, request, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{postId:guid}/images")]
    public async Task<ActionResult<AddImagesResponseDto>> AddImagesToPost(
      [FromRoute] Guid postId,
      [FromBody] AddImagesRequestDto request)
    {
        try
        {
            var result = await creatorPostService.AddImagesToPostAsync(postId, request, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{postId:guid}/text")]
    public async Task<ActionResult<AddTextResponseDto>> AddTextToPost(
      [FromRoute] Guid postId,
      [FromBody] AddTextRequestDto request)
    {
        try
        {
            var result = await creatorPostService.AddTextToPostAsync(postId, request, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Шаг 3: Опубликовать пост (сделать видимым)
    /// </summary>

    [HttpPost("{postId:guid}/publish")]
    public async Task<ActionResult<PublishPostResponseDto>> PublishPost([FromRoute] Guid postId)
    {
        try
        {
            var result = await creatorPostService.PublishPostAsync(postId, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Снять пост с публикации (вернуть в черновики)
    /// </summary>
    [HttpPost("{postId:guid}/unpublish")]
    public async Task<ActionResult<MessageResponseDto>> UnpublishPost([FromRoute] Guid postId)
    {
        try
        {
            var result = await creatorPostService.UnpublishPostAsync(postId, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получить свои посты (опубликованные и черновики)
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<MyPostsResponseDto>> GetMyPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isPublished = null)
    {
        try
        {
            var result = await creatorPostService.GetMyPostsAsync(page, pageSize, isPublished, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    /// <summary>
    /// Получить публичные посты creator'а (только опубликованные, для фронтенда)
    /// </summary>
    [HttpGet("creator/{creatorId:guid}")]
    public async Task<ActionResult<CreatorPostsResponseDto>> GetCreatorPublicPosts(
         [FromRoute] Guid creatorId,
         [FromQuery] int page = 1,
         [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await creatorPostService.GetCreatorPublicPostsAsync(creatorId, page, pageSize);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Загрузить картинки для поста
    /// </summary>
    [HttpPost("upload-images")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<UploadImagesResponseDto>> UploadImages([FromForm] UploadImagesRequestDto request)
    {
        try
        {
            var result = await creatorPostService.UploadImagesAsync(request, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получить изображение поста
    /// </summary>
    [HttpGet("images/{*imagePath}")]
    public async Task<ActionResult<byte[]>> GetImage(string imagePath)
    {
        try
        {
            var bytes = await creatorPostService.GetImageAsync(imagePath);
            return File(bytes, "image/jpeg");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{postId:guid}")]
    public async Task<ActionResult<MessageResponseDto>> DeletePost([FromRoute] Guid postId)
    {
        try
        {
            var result = await creatorPostService.DeletePostAsync(postId, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    /// <summary>
    /// Получить ленту постов от авторов, на которых подписан пользователь
    /// </summary>
    [HttpGet("feed")]
    public async Task<ActionResult<MyPostsResponseDto>> GetSubscriptionFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await creatorPostService.GetSubscriptionFeedAsync(page, pageSize, User);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
