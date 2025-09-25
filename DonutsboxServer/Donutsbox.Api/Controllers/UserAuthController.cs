using Donutsbox.Api.Dto;
using Donutsbox.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserAuthController(IEntityService<UserAuthDto, Guid> service) : ControllerBase
{   /// <summary>
    /// Возвращает все сущности пользователей для аутентификации
    /// </summary>
    /// <returns>Коллекция объектов <see cref="UserAuthDto"/>/></returns>
    /// <response code="200">Список пользователей для аутентификации получен</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserAuthDto>>> Get()
    {
        var users = await service.GetAllAsync();
        return Ok(users);
    }

    /// <summary>
    /// Возвращает cущность пользователя для аутентификации по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Объект <see cref="UserDto"/>.</returns>
    /// <response code="200">Пользователь найден.</response>
    /// <response code="404">Пользователь с указанным ID не найден.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserAuthDto>> Get(Guid id)
    {
        var user = await service.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// Создаёт нового пользователя.
    /// </summary>
    /// <param name="newUser">Данные нового пользователя.</param>
    /// <returns>Созданный объект <see cref="UserAuthDto"/>.</returns>
    /// <response code="200">Пользователь успешно создан.</response>
    [HttpPost]
    public async Task<ActionResult<UserAuthDto>> Post([FromBody] UserAuthDto newUser)
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
    public async Task<IActionResult> Put(Guid id, [FromBody] UserAuthDto updatedUser)
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
}
