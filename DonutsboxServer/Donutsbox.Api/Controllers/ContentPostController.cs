using Donutsbox.Api.Dto;
using Donutsbox.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContentPostController(IEntityService<ContentPostDto, Guid> service) : ControllerBase
{   /// <summary>
    /// Возвращает все посты
    /// </summary>
    /// <returns>Коллекция объектов <see cref="ContentPostDto"/>/></returns>
    /// <response code="200">Список постов получен</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContentPostDto>>> Get()
    {
        var posts = await service.GetAllAsync();
        return Ok(posts);
    }

    /// <summary>
    /// Возвращает пост по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор поста.</param>
    /// <returns>Объект <see cref="ContentPostDto"/>.</returns>
    /// <response code="200">Пост найден.</response>
    /// <response code="404">Пост с указанным ID не найден.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContentPostDto>> Get(Guid id)
    {
        var post = await service.GetByIdAsync(id);
        if (post == null) return NotFound();
        return Ok(post);
    }

    /// <summary>
    /// Создаёт новый пост.
    /// </summary>
    /// <param name="newPost">Данные нового поста.</param>
    /// <returns>Созданный объект <see cref="ContentPostDto"/>.</returns>
    /// <response code="200">Пост успешно создан.</response>
    [HttpPost]
    public async Task<ActionResult<ContentPostDto>> Post([FromBody] ContentPostDto newPost)
    {
        var addedPost = await service.AddAsync(newPost);
        return Ok(addedPost);
    }

    /// <summary>
    /// Обновляет данные существующего поста.
    /// </summary>
    /// <param name="id">Идентификатор поста для обновления.</param>
    /// <param name="updatedPost">Новые данные поста.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Пост успешно обновлён.</response>
    /// <response code="404">Пост с указанным ID не найден.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] ContentPostDto updatedPost)
    {
        var result = await service.UpdateAsync(updatedPost, id);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>
    /// Удаляет пост по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор поста для удаления.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Пост успешно удалён.</response>
    /// <response code="404">Пост с указанным ID не найден.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
