using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.PostCommentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PostCommentController(IPostCommentService service) : ControllerBase
{
    /// <summary>
    /// Создать новый комментарий к посту
    /// </summary>
    /// <param name="dto">Данные комментария</param>
    /// <returns>Созданный комментарий</returns>
    /// <response code="200">Комментарий создан</response>
    /// <response code="400">Пост не найден</response>
    /// <response code="401">Пользователь не авторизован</response>
    [HttpPost]
    public async Task<ActionResult<PostCommentDto>> Post([FromBody] CreatePostCommentDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var comment = await service.AddAsync(dto, userId);
            return Ok(comment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить текст комментария
    /// </summary>
    /// <param name="id">Идентификатор комментария</param>
    /// <param name="request">Объект с новым текстом</param>
    /// <returns>Результат операции</returns>
    /// <response code="200">Комментарий обновлен</response>
    /// <response code="403">Комментарий не принадлежит пользователю</response>
    /// <response code="404">Комментарий не найден</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateCommentRequestDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await service.UpdateAsync(id, request.Text, userId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Удалить комментарий
    /// </summary>
    /// <param name="id">Идентификатор комментария</param>
    /// <returns>Результат операции</returns>
    /// <response code="200">Комментарий удален</response>
    /// <response code="403">Комментарий не принадлежит пользователю</response>
    /// <response code="404">Комментарий не найден</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await service.DeleteAsync(id, userId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Получить все комментарии к посту
    /// </summary>
    /// <param name="postId">Идентификатор поста</param>
    /// <returns>Список комментариев</returns>
    /// <response code="200">Комментарии получены</response>
    [HttpGet("post/{postId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PostCommentDto>>> GetByPostId(Guid postId)
    {
        var comments = await service.GetByPostIdAsync(postId);
        return Ok(comments);
    }
}