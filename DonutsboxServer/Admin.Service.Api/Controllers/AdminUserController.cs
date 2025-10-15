using Admin.Service.Api.Dto;
using Admin.Service.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Service.Api.Controllers;

/// <summary>
/// Контроллер для администрирования пользователей
/// </summary>
[Route("api/admin/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class AdminUserController(IAdminUserService adminUserService, ILogger<AdminUserController> logger) : ControllerBase
{
    private readonly IAdminUserService _adminUserService = adminUserService;
    private readonly ILogger<AdminUserController> _logger = logger;

    /// <summary>
    /// Получить список всех пользователей
    /// </summary>
    /// <returns>Список пользователей с детальной информацией</returns>
    /// <response code="200">Список пользователей получен</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminUserListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminUserListDto>>> GetAllUsers()
    {
        try
        {
            var users = await _adminUserService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка пользователей");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить информацию о пользователе по ID
    /// </summary>
    /// <param name="id">ID пользователя</param>
    /// <returns>Информация о пользователе</returns>
    /// <response code="200">Пользователь найден</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserListDto>> GetUserById(Guid id)
    {
        try
        {
            var user = await _adminUserService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"Пользователь с ID {id} не найден" });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении пользователя {UserId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Удалить пользователя и все его данные
    /// </summary>
    /// <param name="id">ID пользователя</param>
    /// <returns>Результат удаления</returns>
    /// <response code="200">Пользователь успешно удален</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(AdminDeleteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminDeleteResultDto>> DeleteUser(Guid id)
    {
        try
        {
            var result = await _adminUserService.DeleteUserAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Массовое удаление пользователей
    /// </summary>
    /// <param name="userIds">Список ID пользователей для удаления</param>
    /// <returns>Результат удаления</returns>
    /// <response code="200">Операция выполнена</response>
    /// <response code="400">Некорректные данные</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Недостаточно прав (требуется роль Administrator)</response>
    [HttpPost("delete-multiple")]
    [ProducesResponseType(typeof(AdminDeleteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminDeleteResultDto>> DeleteMultipleUsers([FromBody] List<Guid> userIds)
    {
        try
        {
            if (userIds == null || userIds.Count == 0)
            {
                return BadRequest(new { message = "Список ID пользователей пуст" });
            }

            var result = await _adminUserService.DeleteUsersAsync(userIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при массовом удалении пользователей");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}
