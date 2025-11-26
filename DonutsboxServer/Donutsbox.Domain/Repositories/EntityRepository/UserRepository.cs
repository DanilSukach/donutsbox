using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.UserSubscriptionsRepository;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.EntityRepository;

public class UserRepository(DonutsboxDbContext context) : IEntityRepository<User, Guid>, IUserSubscriptionsRepository
{
    public async Task<User> AddAsync(User entity)
    {
        context.Users.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        context.Users.Remove(oldValue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<User?> GetByIdUserWithSubscriptionsAsync(Guid id) => await context.Users
        .Include(u => u.UserSubscriptions)
            .ThenInclude(us => us.Subscription)
                .ThenInclude(s => s.CreatorPageData)
                    .ThenInclude(cpd => cpd.User)
                        .ThenInclude(u => u.UserData)
        .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<IEnumerable<User>> GetAllAsync() => await context.Users.ToListAsync();

    public async Task<User?> GetByIdAsync(Guid id) => await context.Users
        .Include(u => u.UserData)
        .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<bool> UpdateAsync(User entity, Guid id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        oldValue.Name = entity.Name;
        oldValue.UserTypeId = entity.UserTypeId;
        oldValue.UserAuthId = entity.UserAuthId;
        if (entity.CreatorPageData != null)
        {
            if (oldValue.CreatorPageData != null)
            {
                oldValue.CreatorPageData.PageName = entity.CreatorPageData.PageName;
                oldValue.CreatorPageData.BannerURL = entity.CreatorPageData.BannerURL;
                oldValue.CreatorPageData.AvatarURL = entity.CreatorPageData.AvatarURL;
                oldValue.CreatorPageData.Description = entity.CreatorPageData.Description;
                oldValue.CreatorPageData.SubscribersCount = entity.CreatorPageData.SubscribersCount;
            }
            else
            {
                oldValue.CreatorPageData = entity.CreatorPageData;
            }
        }
        await context.SaveChangesAsync();
        return true;
    }
}