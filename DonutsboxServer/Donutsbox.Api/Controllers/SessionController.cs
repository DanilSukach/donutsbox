using Donutsbox.Api.Dto;
using Donutsbox.Domain.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SessionController(DonutsboxDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<SessionInfoDto>> GetMe(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);

        var user = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.UserAuth)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            return Unauthorized();

        var session = new SessionInfoDto
        {
            UserId = user.Id,
            DisplayName = user.Name,
            Email = user.UserAuth.AuthEmail,
            Role = user.UserType.Name,
            IsCreator = string.Equals(user.UserType.Name, "Creator", StringComparison.OrdinalIgnoreCase),
            HasCreatorPage = user.CreatorPageData != null,
            CreatorPageId = user.CreatorPageData?.Id
        };

        return Ok(session);
    }
}

