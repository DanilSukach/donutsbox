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
    /// Подписывает пользователя на подписку
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("subscribe-user")]
    public async Task<ActionResult<UserSubscriptionDto>> SubscribeUserAsync([FromBody] UserSubscriptionCreateDto dto)
    {
        try
        {
            var subscription = await userInteractionService.SubscribeUserAsync(dto, User);
            return Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

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
}
