using Auth.Api.Dto;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class AdminController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Создает нового администратора
    /// </summary>
    /// <param name="request">Данные для создания администратора</param>
    /// <returns>Результат создания</returns>
    /// <response code="200">Администратор успешно создан</response>
    /// <response code="400">Ошибка валидации данных</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpPost("create")]
    public async Task<IActionResult> CreateAdmin([FromBody] RegisterRequestDto request)
    {
        try
        {
            await authService.CreateAdminAsync(request);
            return Ok(new { message = "Administrator created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получает информацию о текущем администраторе
    /// </summary>
    /// <returns>Информация об администраторе</returns>
    /// <response code="200">Информация получена</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpGet("profile")]
    public IActionResult GetAdminProfile()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            Email = email,
            Role = role
        });
    }
}
