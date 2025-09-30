using Donutsbox.Api.Dto;
using Donutsbox.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CreatorPageDataController(IEntityService<CreatorPageDataDto, Guid> service) : ControllerBase
{   /// <summary>
    /// Возвращает все страницы авторов
    /// </summary>
    /// <returns>Коллекция объектов <see cref="CreatorPageDataDto"/>/></returns>
    /// <response code="200">Список страниц авторов получен</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CreatorPageDataDto>>> Get()
    {
        var pages = await service.GetAllAsync();
        return Ok(pages);
    }

    /// <summary>
    /// Возвращает страницу автора по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор страницы.</param>
    /// <returns>Объект <see cref="CreatorPageDataDto"/>.</returns>
    /// <response code="200">Страница найдена.</response>
    /// <response code="404">Страница с указанным ID не найдена.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreatorPageDataDto>> Get(Guid id)
    {
        var page = await service.GetByIdAsync(id);
        if (page == null) return NotFound();
        return Ok(page);
    }

    /// <summary>
    /// Создаёт нового страницу автора.
    /// </summary>
    /// <param name="newPage">Данные новой страницы автора.</param>
    /// <returns>Созданный объект <see cref="CreatorPageDataDto"/>.</returns>
    /// <response code="200">Страница успешно создана.</response>
    [HttpPost]
    public async Task<ActionResult<CreatorPageDataDto>> Post([FromBody] CreatorPageDataDto newPage)
    {
        var addedPage = await service.AddAsync(newPage);
        return Ok(addedPage);
    }

    /// <summary>
    /// Обновляет данные существующей страницы.
    /// </summary>
    /// <param name="id">Идентификатор страницы для обновления.</param>
    /// <param name="updatedPage">Новые данные страницы.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Страница успешно обновлена.</response>
    /// <response code="404">Страница с указанным ID не найдена.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] CreatorPageDataDto updatedPage)
    {
        var result = await service.UpdateAsync(updatedPage, id);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>
    /// Удаляет страницу по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор страницы для удаления.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Страница успешно удалена.</response>
    /// <response code="404">Страница с указанным ID не найдена.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
