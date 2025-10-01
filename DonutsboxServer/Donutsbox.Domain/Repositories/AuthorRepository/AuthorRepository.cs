using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.AuthorRepository;

public class AuthorRepository(DonutsboxDbContext context) : IAuthorRepository
{
    public async Task<(bool, string?)> AddAsync(CreatorPageData creatorPageData)
    {
        try
        {
            await context.CreatorsPageData.AddAsync(creatorPageData);
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException dbEx)
        {
            return (false, dbEx.InnerException?.Message ?? dbEx.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

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
            .FirstOrDefaultAsync(u => u.Id == id && u.UserTypeId == 2);
    }

    public async Task<IEnumerable<User>> GetTopBySubscribersAsync(int count)
    {
        return await context.Users
            .Include(u => u.CreatorPageData)
            .Where(u => u.UserTypeId == 2)
            .OrderByDescending(u => u.CreatorPageData!.SubscribersCount)
            .Take(count)
            .ToListAsync();
    }

    
}
