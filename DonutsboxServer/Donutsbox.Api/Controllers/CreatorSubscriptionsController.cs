using Donutsbox.Api.Dto;
using Donutsbox.Domain.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Donutsbox.Api.Controllers;

[Route("api/creator-subscriptions")]
[ApiController]
[Authorize(Roles = "Creator")]
public class CreatorSubscriptionsController(DonutsboxDbContext db) : ControllerBase
{
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetMySubscriptions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);

        var creator = await db.Users
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (creator?.CreatorPageData == null)
            return BadRequest(new { message = "Creator page not found" });

        var subscriptions = await db.Subscriptions
            .Where(s => s.CreatorPageDataId == creator.CreatorPageData.Id)
            .Include(s => s.SubscriptionPeriod)
            .Select(s => new SubscriptionDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Price = s.Price,
                PictureURL = s.PictureURL,
                SubscriptionPeriodId = s.SubscriptionPeriodId,
                SubscriptionPeriodMonths = s.SubscriptionPeriod.Months,
                MonthlyPrice = s.Price,
                ParentSubscriptionId = s.ParentSubscriptionId,
                CreatorPageDataId = s.CreatorPageDataId
            })
            .ToListAsync();

        var uniqueSubscriptions = subscriptions
            .GroupBy(s => s.Name)
            .Select(g => g.First())
            .ToList();

        return Ok(uniqueSubscriptions);
    }
}

