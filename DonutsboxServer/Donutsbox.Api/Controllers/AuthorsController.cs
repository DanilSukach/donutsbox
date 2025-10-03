using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.AuthorService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuthorsController(IAuthorService authorService) : ControllerBase
{
    /// <summary>
    /// Получает информацию об авторах
    /// </summary>
    /// <returns>Информация об авторах</returns>
    /// <response code="200">Информация получена</response>
    /// <response code="401">Не авторизован</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuthorRequestDto>>> GetAuthors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false)
    {
        var authors = await authorService.GetAuthorsAsync(page, pageSize, sortBy, descending);
        return Ok(authors);
    }

    /// <summary>
    /// Добавление страницы автора
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>Страница автора</returns>
    [HttpPost("creator")]
    [Authorize (Roles = "Creator")]
    public async Task<ActionResult<CreatorPageDataDto>> AddCreatorPage([FromBody] CreatorPageDataDto dto)
    {
        try
        {
            var page = await authorService.AddCreatorPageAsync(dto, User);
            return Ok(page);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Добавление подписки автора
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>Страница автора</returns>
    [HttpPost("subscription")]
    [Authorize(Roles = "Creator")]
    public async Task<ActionResult<SubscriptionDto>> AddSubscriptionAsync([FromBody] SubscriptionCreateDto dto)
    {
        try
        {
            var page = await authorService.AddSubscriptionAsync(dto, User);
            return Ok(page);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    /// <summary>
    /// Получает информацию об авторе
    /// </summary>
    /// <returns>Информация об авторе</returns>
    /// <response code="200">Информация получена</response>
    /// <response code="401">Не авторизован</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuthorRequestDto>> GetAuthorById(Guid id)
    {
        var author = await authorService.GetAuthorByIdAsync(id);
        if (author == null)
            return NotFound();

        return Ok(author);
    }

    /// <summary>
    /// Получает информацию о топе авторах
    /// </summary>
    /// <returns>Информация о топе авторах</returns>
    /// <response code="200">Информация получена</response>
    /// <response code="401">Не авторизован</response>
    [HttpGet("top")]
    public async Task<ActionResult<IEnumerable<AuthorRequestDto>>> GetTopAuthors([FromQuery] int count = 10)
    {
        var authors = await authorService.GetTopAuthorsAsync(count);
        return Ok(authors);
    }

    /// <summary>
    /// Получает информацию о топе подписчиков, которые подписаны на автора
    /// </summary>
    /// <returns>Информация о топе поддерживаемых авторах</returns>
    /// <response code="200">Информация получена</response>
    /// <response code="401">Не авторизован</response>
    [HttpGet("top-supported")]
    public async Task<ActionResult<IEnumerable<UserRequestDto>>> GetTopSupportedUsers([FromQuery] int count = 10)
    {
        try
        {
            var authors = await authorService.GetTopSupportedUsersAsync(User, count);
            return Ok(authors);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
