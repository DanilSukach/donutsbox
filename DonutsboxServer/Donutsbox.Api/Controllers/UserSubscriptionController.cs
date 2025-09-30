using Donutsbox.Api.Dto;
using Donutsbox.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserSubscriptionController(IEntityService<UserSubscriptionDto, Guid> service) : ControllerBase
{   /// <summary>
    /// Возвращает все подписки пользователей
    /// </summary>
    /// <returns>Коллекция объектов <see cref="UserSubscriptionDto"/>/></returns>
    /// <response code="200">Список подписок получен</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSubscriptionDto>>> Get()
    {
        var subscription = await service.GetAllAsync();
        return Ok(subscription);
    }

    /// <summary>
    /// Возвращает подписку по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор подписки.</param>
    /// <returns>Объект <see cref="UserSubscriptionDto"/>.</returns>
    /// <response code="200">Подписка найдена.</response>
    /// <response code="404">Подписка с указанным ID не найдена.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSubscriptionDto>> Get(Guid id)
    {
        var subscription = await service.GetByIdAsync(id);
        if (subscription == null) return NotFound();
        return Ok(subscription);
    }

    /// <summary>
    /// Создаёт новую подписку.
    /// </summary>
    /// <param name="newSubscription">Данные новой подписки.</param>
    /// <returns>Созданный объект <see cref="UserSubscriptionDto"/>.</returns>
    /// <response code="200">Подписка успешно создана.</response>
    [HttpPost]
    public async Task<ActionResult<UserSubscriptionDto>> Post([FromBody] UserSubscriptionDto newSubscription)
    {
        var addedSubscription = await service.AddAsync(newSubscription);
        return Ok(addedSubscription);
    }

    /// <summary>
    /// Обновляет данные существующей подписки.
    /// </summary>
    /// <param name="id">Идентификатор подписки для обновления.</param>
    /// <param name="updatedSubscription">Новые данные подписки.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Подписка успешно обновлена.</response>
    /// <response code="404">Подписка с указанным ID не найдена.</response>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UserSubscriptionDto updatedSubscription)
    {
        var result = await service.UpdateAsync(updatedSubscription, id);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>
    /// Удаляет подписку по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор подписки для удаления.</param>
    /// <returns>Код результата.</returns>
    /// <response code="200">Подписка успешно удалёна.</response>
    /// <response code="404">Подписка с указанным ID не найдена.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
