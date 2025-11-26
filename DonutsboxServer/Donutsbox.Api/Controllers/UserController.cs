using Donutsbox.Api.Dto;
using Donutsbox.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IUserService service) : ControllerBase
{   /// <summary>
    /// Возвращает всех пользователей
    /// </summary>
    /// <returns>Коллекция объектов <see cref="UserDto"/>/></returns>
    /// <response code="200">Список пользователей получен</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> Get()
    {
        var users = await service.GetAllAsync();
        return Ok(users);
    }

    /// <summary>
    /// Возвращает пользователя по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Объект <see cref="UserDto"/>.</returns>
    /// <response code="200">Пользователь найден.</response>
    /// <response code="404">Пользователь с указанным ID не найден.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id)
    {
        var user = await service.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// Создаёт нового пользователя.
    /// </summary>
    /// <param name="newUser">Данные нового пользователя.</param>
    /// <returns>Созданный объект <see cref="UserDto"/>.</returns>
    /// <response code="200">Пользователь успешно создан.</response>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Post([FromBody] UserDto newUser)
    {
        var addedUser = await service.AddAsync(newUser);
        return Ok(addedUser);
    }

    /// <summary>
    /// Обновляет данные существующего пользователя.
    /// </summary>
    /// <param name="id">Идентификатор пользователя для обновления.</param>
    /// <param name="updatedUser">Новые данные пользователя.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Пользователь успешно обновлён.</response>
    /// <response code="404">Пользователь с указанным ID не найден.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UserDto updatedUser)
    {
        var result = await service.UpdateAsync(updatedUser, id);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>
    /// Удаляет пользователя по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пользователя для удаления.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Пользователь успешно удалён.</response>
    /// <response code="404">Пользователь с указанным ID не найден.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>
    /// Изменяет имя пользователя
    /// </summary>
    /// <param name="dto">Новое имя пользователя</param>
    /// <returns>Результат операции</returns>
    /// <response code="200">Имя успешно изменено</response>
    /// <response code="400">Ошибка валидации</response>
    [HttpPut("user-name")]
    [Authorize]
    public async Task<ActionResult> ChangeUserName([FromBody] UserNameDto dto)
    {
        try
        {
            var result = await service.ChangeUserName(dto, User);
            return Ok(new { success = result, message = "Имя успешно изменено" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
