using Donutsbox.Api.Dto;
using Donutsbox.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IEntityService<UserDto, Guid> service) : ControllerBase
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id)
    {
        var user = await service.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }
    [HttpPost]
    public async Task<ActionResult<UserDto>> Post([FromBody] UserDto newUser)
    {
        var addedUser = await service.AddAsync(newUser);
        return Ok(addedUser);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UserDto updatedUser)
    {
        var result = await service.UpdateAsync(updatedUser, id);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
