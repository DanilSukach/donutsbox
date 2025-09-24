using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories;

namespace Donutsbox.Api.Services;

public class SubscriptionService(IEntityRepository<Subscription, Guid> repository, IMapper mapper) : IEntityService<SubscriptionDto, Guid>
{
    public async Task<SubscriptionDto?> AddAsync(SubscriptionDto entity)
    {
        var subscription = mapper.Map<Subscription>(entity);
        var addedSubscription = await repository.AddAsync(subscription);
        return mapper.Map<SubscriptionDto>(addedSubscription);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var subscription = await repository.GetByIdAsync(id);
        if (subscription == null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<IEnumerable<SubscriptionDto>> GetAllAsync()
    {
        var subscriptions = await repository.GetAllAsync();
        return subscriptions.Select(mapper.Map<SubscriptionDto>);
    }

    public async Task<SubscriptionDto?> GetByIdAsync(Guid id)
    {
        var subscription = await repository.GetByIdAsync(id);
        return mapper.Map<SubscriptionDto>(subscription);
    }

    public async Task<bool> UpdateAsync(SubscriptionDto entity, Guid id)
    {
        var existingSubscription = await repository.GetByIdAsync(id);
        if (existingSubscription == null) return false;

        var updatedSubscription = mapper.Map<Subscription>(entity);
        updatedSubscription.SubscriptionId = id;

        return await repository.UpdateAsync(updatedSubscription, id);
    }
}
