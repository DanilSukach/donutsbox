using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.UserInteractionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserInteractionController(IUserInteractionService userInteractionService) : ControllerBase
{
    /// <summary>
    /// Отписывает пользователя от создателя контента
    /// </summary>
    /// <param name="creatorUserId">ID пользователя-создателя от которого отписываемся</param>
    /// <returns></returns>
    [HttpDelete("unsubscribe-user/{creatorUserId}")]
    public async Task<ActionResult> UnsubscribeUserAsync(Guid creatorUserId)
    {
        try
        {
            await userInteractionService.UnsubscribeUserAsync(creatorUserId, User);
            return Ok(new { message = "Successfully unsubscribed" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Оставляет реакцию на пост
    /// </summary>
    /// <param name="dto">DTO которое содержит id поста и тип реакции</param>
    /// <returns></returns>
    [HttpPost("change-reaction")]
    public async Task<ActionResult> ChangeReactionAsync([FromBody] ContentPostReactionDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });

            var changed = await userInteractionService.ChangeReactionAsync(User, dto);

            if (changed)
                return Ok(new { message = "Reaction saved" });
            else
                return NotFound(new { message = "Post or reaction type not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
