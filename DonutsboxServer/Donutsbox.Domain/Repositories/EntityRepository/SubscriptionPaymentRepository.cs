using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.EntityRepository;

public class SubscriptionPaymentRepository(DonutsboxDbContext context) : IEntityRepository<SubscriptionPayment, Guid>
{
    public async Task<SubscriptionPayment> AddAsync(SubscriptionPayment entity)
    {
        context.SubscriptionPayments.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        context.SubscriptionPayments.Remove(existing);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SubscriptionPayment>> GetAllAsync()
    {
        return await context.SubscriptionPayments
            .Include(p => p.Subscription)
            .ThenInclude(s => s.CreatorPageData)
            .Include(p => p.User)
            .ToListAsync();
    }

    public async Task<SubscriptionPayment?> GetByIdAsync(Guid id)
    {
        return await context.SubscriptionPayments
            .Include(p => p.Subscription)
                .ThenInclude(s => s.CreatorPageData)
            .Include(p => p.User)
            .Include(p => p.UserSubscription)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> UpdateAsync(SubscriptionPayment entity, Guid id)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        existing.PaymentId = entity.PaymentId;
        existing.Status = entity.Status;
        existing.Amount = entity.Amount;
        existing.Currency = entity.Currency;
        existing.ConfirmationUrl = entity.ConfirmationUrl;
        existing.Description = entity.Description;
        existing.ExpiresAt = entity.ExpiresAt;
        existing.MetadataJson = entity.MetadataJson;
        existing.IdempotenceKey = entity.IdempotenceKey;
        existing.UserSubscriptionId = entity.UserSubscriptionId;

        await context.SaveChangesAsync();
        return true;
    }
}

