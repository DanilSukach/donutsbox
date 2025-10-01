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
}
