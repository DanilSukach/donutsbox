using Auth.Api.Dto;
using Auth.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            await auth.RegisterAsync(request);
            return Ok(new { message = "Registered" });
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message switch
            {
                "Email exists" => Conflict(new { message = ex.Message }),
                "Password doesn't match" => BadRequest(new { message = ex.Message }),
                "Administrator role cannot be created through registration" => Forbid(ex.Message),
                _ => BadRequest(new { message = ex.Message })
            };
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var tokens = await auth.LoginAsync(request);
            return Ok(tokens);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid credentials");
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto refreshToken)
    {
        try
        {
            var tokens = await auth.RefreshTokenAsync(refreshToken);
            return Ok(tokens);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid refresh token");
        }
    }
}
