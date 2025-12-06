using Admin.Service.Api.Dto;
using Admin.Service.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Service.Api.Controllers;

/// <summary>
/// Контроллер для администрирования контента
/// </summary>
[Route("api/admin/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class AdminContentController(IAdminContentService adminContentService, ILogger<AdminContentController> logger) : ControllerBase
{

    /// <summary>
    /// Получить список всех постов
    /// </summary>
    /// <returns>Список постов с детальной информацией</returns>
    /// <response code="200">Список постов получен</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpGet("posts")]
    [ProducesResponseType(typeof(IEnumerable<AdminContentPostListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminContentPostListDto>>> GetAllPosts()
    {
        try
        {
            var posts = await adminContentService.GetAllPostsAsync();
            return Ok(posts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка постов");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить информацию о посте по ID
    /// </summary>
    /// <param name="id">ID поста</param>
    /// <returns>Информация о посте</returns>
    /// <response code="200">Пост найден</response>
    /// <response code="404">Пост не найден</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpGet("posts/{id:guid}")]
    [ProducesResponseType(typeof(AdminContentPostListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminContentPostListDto>> GetPostById(Guid id)
    {
        try
        {
            var post = await adminContentService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound(new { message = $"Пост с ID {id} не найден" });
            }
            return Ok(post);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении поста {PostId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить пост
    /// </summary>
    /// <param name="id">ID поста</param>
    /// <returns>Результат удаления</returns>
    /// <response code="200">Пост успешно удален</response>
    /// <response code="404">Пост не найден</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpDelete("posts/{id:guid}")]
    [ProducesResponseType(typeof(AdminDeleteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminDeleteResultDto>> DeletePost(Guid id)
    {
        try
        {
            var result = await adminContentService.DeletePostAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении поста {PostId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Массовое удаление постов
    /// </summary>
    /// <param name="postIds">Список ID постов для удаления</param>
    /// <returns>Результат удаления</returns>
    /// <response code="200">Операция выполнена</response>
    /// <response code="400">Некорректные данные</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpPost("posts/delete-multiple")]
    [ProducesResponseType(typeof(AdminDeleteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminDeleteResultDto>> DeleteMultiplePosts([FromBody] List<Guid> postIds)
    {
        try
        {
            if (postIds == null || postIds.Count == 0)
            {
                return BadRequest(new { message = "Список ID постов пуст" });
            }

            var result = await adminContentService.DeletePostsAsync(postIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при массовом удалении постов");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить все посты создателя
    /// </summary>
    /// <param name="creatorId">ID страницы создателя (CreatorPageDataId)</param>
    /// <returns>Результат удаления</returns>
    /// <response code="200">Операция выполнена</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpDelete("creator/{creatorId:guid}/posts")]
    [ProducesResponseType(typeof(AdminDeleteResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDeleteResultDto>> DeleteCreatorPosts(Guid creatorId)
    {
        try
        {
            var result = await adminContentService.DeleteCreatorPostsAsync(creatorId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении постов создателя {CreatorId}", creatorId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Добавить пост в теневой бан
    /// </summary>
    /// <param name="postId">ID поста</param>
    /// <returns>Результат операции</returns>
    /// <response code="200">Пост добавлен в теневой бан</response>
    /// <response code="404">Пост не найден</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpPost("posts/{postId:guid}/shadowban")]
    [ProducesResponseType(typeof(AdminActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminActionResponseDto>> ShadowBanPost(Guid postId)
    {
        try
        {
            var result = await adminContentService.ShadowBanPostAsync(postId);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении поста {PostId} в теневой бан", postId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Снять теневой бан с поста
    /// </summary>
    /// <param name="postId">ID поста</param>
    /// <returns>Результат операции</returns>
    /// <response code="200">Теневой бан снят</response>
    /// <response code="404">Пост не найден</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpPost("posts/{postId:guid}/unshadowban")]
    [ProducesResponseType(typeof(AdminActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminActionResponseDto>> UnshadowBanPost(Guid postId)
    {
        try
        {
            var result = await adminContentService.UnshadowBanPostAsync(postId);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при снятии теневого бана с поста {PostId}", postId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}
