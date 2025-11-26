using Auth.Api.Dto;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserProfileController(IUserProfileService service) : ControllerBase
{
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] NewPasswordDto dto)
    {
        try
        {
            await service.ChangePassword(dto, User);
            return Ok(new { success = true, message = "Password changed successfully" });
        }
        catch(UnauthorizedAccessException e)
        {
            return Unauthorized(new { success = false, message = e.Message });
        }
        catch(InvalidOperationException e)
        {
            return BadRequest(new { success = false, message = e.Message });
        }
    }

    [HttpPut("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] NewEmailDto dto)
    {
        try
        {
            await service.ChangeEmail(dto, User);
            return Ok();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }
}
