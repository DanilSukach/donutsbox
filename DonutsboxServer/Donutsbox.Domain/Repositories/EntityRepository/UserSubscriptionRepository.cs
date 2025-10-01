using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.EntityRepository;

public class UserSubscriptionRepository(DonutsboxDbContext context) : IEntityRepository<UserSubscription, Guid>
{
    public async Task<UserSubscription> AddAsync(UserSubscription entity)
    {
        context.UsersSubscriptions.Add(entity);
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
        context.UsersSubscriptions.Remove(oldValue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UserSubscription>> GetAllAsync() => await context.UsersSubscriptions.ToListAsync();

    public async Task<UserSubscription?> GetByIdAsync(Guid id) => await context.UsersSubscriptions.FirstOrDefaultAsync(us => us.Id == id);

    public async Task<bool> UpdateAsync(UserSubscription entity, Guid id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        oldValue.UserId = entity.UserId;
        oldValue.SubscriptionId = entity.SubscriptionId;
        oldValue.BeginDate = entity.BeginDate;
        oldValue.EndDate = entity.EndDate;
        await context.SaveChangesAsync();
        return true;
    }
}


