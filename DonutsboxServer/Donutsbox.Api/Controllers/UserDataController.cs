using Donutsbox.Api.Dto;
using Donutsbox.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserDataController(IEntityService<UserDataDto, Guid> service) : ControllerBase
{   /// <summary>
    /// Возвращает данные всех пользователей
    /// </summary>
    /// <returns>Коллекция объектов <see cref="UserDataDto"/>/></returns>
    /// <response code="200">Список данных о пользователях получен</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDataDto>>> Get()
    {
        var users = await service.GetAllAsync();
        return Ok(users);
    }

    /// <summary>
    /// Возвращает данные о пользователе по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор данных пользователя.</param>
    /// <returns>Объект <see cref="UserDataDto"/>.</returns>
    /// <response code="200">Даныне найдены.</response>
    /// <response code="404">Данные о пользователе с указанным ID не найден.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDataDto>> Get(Guid id)
    {
        var user = await service.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// Создаёт новые данные пользователя.
    /// </summary>
    /// <param name="newUser">Данные нового пользователя.</param>
    /// <returns>Созданный объект <see cref="UserDataDto"/>.</returns>
    /// <response code="200">Данные о пользователе успешно созданы.</response>
    [HttpPost]
    public async Task<ActionResult<UserDataDto>> Post([FromBody] UserDataDto newUser)
    {
        var addedUser = await service.AddAsync(newUser);
        return Ok(addedUser);
    }

    /// <summary>
    /// Обновляет данные существующего пользователя.
    /// </summary>
    /// <param name="id">Идентификатор данных о пользователе для обновления.</param>
    /// <param name="updatedUser">Новые данные пользователя.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Пользователь успешно обновлён.</response>
    /// <response code="404">Пользователь с указанным ID не найден.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UserDataDto updatedUser)
    {
        var result = await service.UpdateAsync(updatedUser, id);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>
    /// Удаляет данные пользователя по идентификатору.
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
