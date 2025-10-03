using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.EntityRepository;

public class SubscriptionPeriodRepository(DonutsboxDbContext context) : IEntityRepository<SubscriptionPeriod, int>
{
    public async Task<SubscriptionPeriod> AddAsync(SubscriptionPeriod entity)
    {
        context.SubscriptionPeriods.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        context.SubscriptionPeriods.Remove(oldValue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SubscriptionPeriod>> GetAllAsync() => await context.SubscriptionPeriods.ToListAsync();

    public async Task<SubscriptionPeriod?> GetByIdAsync(int id) => await context.SubscriptionPeriods.FirstOrDefaultAsync(ut => ut.Id == id);

    public async Task<bool> UpdateAsync(SubscriptionPeriod entity, int id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        oldValue.Months = entity.Months;
        await context.SaveChangesAsync();
        return true;
    }
}
