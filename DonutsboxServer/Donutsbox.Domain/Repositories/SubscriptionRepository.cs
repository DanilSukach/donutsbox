using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class SubscriptionRepository(DonutsboxDbContext context) : IEntityRepository<Subscription, Guid>
{
    public async Task<Subscription> AddAsync(Subscription entity)
    {
        var subscription = await context.Subscriptions.AddAsync(entity);
        await context.SaveChangesAsync();
        return subscription.Entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var subscription = await GetByIdAsync(id);
        if (subscription == null) return false;
        context.Subscriptions.Remove(subscription);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Subscription>> GetAllAsync()
    {
        return await context.Subscriptions.ToListAsync();
    }

    public async Task<Subscription?> GetByIdAsync(Guid id)
    {
        return await context.Subscriptions
                            .FirstOrDefaultAsync(s => s.SubscriptionId == id);
    }

    public async Task<bool> UpdateAsync(Subscription entity, Guid id)
    {
        var subscription = await GetByIdAsync(id);
        if (subscription == null) return false;
        subscription.Name = entity.Name;
        subscription.PageId = entity.PageId;
        subscription.Price = entity.Price;
        subscription.Description = entity.Description;
        subscription.PictureURL = entity.PictureURL;
        await context.SaveChangesAsync();
        return true;
    }
}
