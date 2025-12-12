using Auth.Api.Dto;
using Auth.Api.Services;
using Donutsbox.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService auth, IConfiguration configuration) : ControllerBase
{
    private readonly int cookieLifetimeMinutes = configuration.GetValue<int?>("Jwt:AccessTokenLifetimeMinutes") ?? 60;

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
            // Определяем тип ошибки для правильного HTTP статуса
            var statusCode = ex.Message switch
            {
                "Email is required" => StatusCodes.Status400BadRequest,
                "Invalid email format" => StatusCodes.Status400BadRequest,
                "Email exists" => StatusCodes.Status409Conflict,
                "Password doesn't match" => StatusCodes.Status400BadRequest,
                "Password is required" => StatusCodes.Status400BadRequest,
                "Password must be at least 8 characters long" => StatusCodes.Status400BadRequest,
                "Password must not exceed 128 characters" => StatusCodes.Status400BadRequest,
                "Password cannot consist only of digits" => StatusCodes.Status400BadRequest,
                "Password must contain at least one digit or special character" => StatusCodes.Status400BadRequest,
                "Password must contain at least one uppercase letter" => StatusCodes.Status400BadRequest,
                "Password must contain at least one lowercase letter" => StatusCodes.Status400BadRequest,
                "Password must contain at least one digit" => StatusCodes.Status400BadRequest,
                "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)" => StatusCodes.Status400BadRequest,
                "Administrator role cannot be created through registration" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, new { 
                message = ex.Message,
                field = GetErrorField(ex.Message) // Добавляем поле для фронтенда
            });
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
            AppendAuthCookie(tokens.AccessToken);
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
            AppendAuthCookie(tokens.AccessToken);
            return Ok(tokens);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid refresh token");
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        DeleteAuthCookie();
        return Ok(new { message = "Logged out" });
    }

    private void AppendAuthCookie(string accessToken)
    {
        var options = BuildCookieOptions(DateTimeOffset.UtcNow.AddMinutes(cookieLifetimeMinutes));
        Response.Cookies.Append(AuthConstants.JwtCookieName, accessToken, options);
    }

    private void DeleteAuthCookie()
    {
        var options = BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1));
        Response.Cookies.Delete(AuthConstants.JwtCookieName, options);
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset? expires)
    {
        var isHttps = HttpContext.Request.IsHttps;
        var host = HttpContext.Request.Host.Host;
        
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            Expires = expires
        };
        
        // Устанавливаем домен только для production домена (не для localhost)
        if (isHttps && !host.Contains("localhost") && !host.Contains("127.0.0.1") && !host.Contains("192.168"))
        {
            // Извлекаем домен из host (убираем порт если есть)
            var domain = host;
            if (domain.Contains(':'))
            {
                domain = domain.Split(':')[0];
            }
            
            // Убираем www. префикс для установки cookie на корневой домен
            if (domain.StartsWith("www."))
            {
                domain = domain.Substring(4);
            }
            
            options.Domain = domain;
        }
        
        return options;
    }

    // Вспомогательный метод для определения поля ошибки
    private static string? GetErrorField(string errorMessage)
    {
        return errorMessage switch
        {
            "Email is required" => "authEmail",
            "Invalid email format" => "authEmail",
            "Email exists" => "authEmail",
            "Password is required" => "password",
            "Password must be at least 8 characters long" => "password",
            "Password must not exceed 128 characters" => "password",
            "Password cannot consist only of digits" => "password",
            "Password must contain at least one digit or special character" => "password",
            "Password must contain at least one uppercase letter" => "password",
            "Password must contain at least one lowercase letter" => "password",
            "Password must contain at least one digit" => "password",
            "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)" => "password",
            "Password doesn't match" => "repeatPassword",
            _ => null
        };
    }
}
