using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.AuthorRepository;

public class AuthorRepository(DonutsboxDbContext context) : IAuthorRepository
{
    public async Task<IEnumerable<User>> GetAllAsync(
     int page,
     int pageSize,
     string? sortBy = null,
     bool descending = false)
    {
        IQueryable<User> query = context.Users
            .Include(u => u.CreatorPageData)
            .Where(u => u.UserTypeId == 2);

        if (!string.IsNullOrEmpty(sortBy))
        {
            query = sortBy switch
            {
                "subscribers" => descending
                    ? query.OrderByDescending(a => a.CreatorPageData!.SubscribersCount)
                    : query.OrderBy(a => a.CreatorPageData!.SubscribersCount),
                "name" => descending
                    ? query.OrderByDescending(a => a.Name)
                    : query.OrderBy(a => a.Name),
                _ => query
            };
        }

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await context.Users
            .Include(u => u.CreatorPageData)
            .ThenInclude(s => s!.Subscriptions)
            .ThenInclude(sp => sp.SubscriptionPeriod)
            .FirstOrDefaultAsync(u => u.Id == id && u.UserTypeId == 2);
    }

    public async Task<IEnumerable<User>> GetTopBySubscribersAsync(int count)
    {
        return await context.Users
            .Include(u => u.CreatorPageData)
            .ThenInclude(c => c!.Subscriptions)
            .Where(u => u.UserTypeId == 2)
            .OrderByDescending(u => u.CreatorPageData!.SubscribersCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetTopSupportedUsersAsync(Guid creatorPageId, int count)
    {
        return await context.UsersSubscriptions
            .Where(us => us.Subscription.CreatorPageDataId == creatorPageId) 
            .Include(us => us.User)
            .OrderByDescending(us => us.BeginDate)
            .Take(count)
            .Select(us => us.User)
            .ToListAsync();
    }

}
