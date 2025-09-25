using Donutsbox.Api.Dto;
using Donutsbox.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserTypeController(IEntityService<UserTypeDto, int> service) : ControllerBase
{   /// <summary>
    /// Возвращает все типы пользователей
    /// </summary>
    /// <returns>Коллекция объектов <see cref="UserTypeDto"/>/></returns>
    /// <response code="200">Список типов пользователей получен</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserTypeDto>>> Get()
    {
        var userTypes = await service.GetAllAsync();
        return Ok(userTypes);
    }

    /// <summary>
    /// Возвращает тип пользователя по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор типа пользователя.</param>
    /// <returns>Объект <see cref="UserTypeDto"/>.</returns>
    /// <response code="200">Тип пользователя найден.</response>
    /// <response code="404">Тип пользователя с указанным ID не найден.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserTypeDto>> Get(int id)
    {
        var userType = await service.GetByIdAsync(id);
        if (userType == null) return NotFound();
        return Ok(userType);
    }

    /// <summary>
    /// Создаёт новый тип пользователя.
    /// </summary>
    /// <param name="newType">Данные нового типа пользоваетля.</param>
    /// <returns>Созданный объект <see cref="UserTypeDto"/>.</returns>
    /// <response code="200">Тип пользователя успешно создан.</response>
    [HttpPost]
    public async Task<ActionResult<UserTypeDto>> Post([FromBody] UserTypeDto newType)
    {
        var addedType = await service.AddAsync(newType);
        return Ok(addedType);
    }

    /// <summary>
    /// Обновляет данные существующего типа пользователя.
    /// </summary>
    /// <param name="id">Идентификатор типа пользователя для обновления.</param>
    /// <param name="updatedType">Новые данные типа пользователя.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Тип успешно обновлён.</response>
    /// <response code="404">Тип пользователя с указанным ID не найден.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(int id, [FromBody] UserTypeDto updatedType)
    {
        var result = await service.UpdateAsync(updatedType, id);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>
    /// Удаляет тип пользователя по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор типа пользователя для удаления.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Тип пользователя успешно удалён.</response>
    /// <response code="404">Тип пользователя с указанным ID не найден.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await service.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
