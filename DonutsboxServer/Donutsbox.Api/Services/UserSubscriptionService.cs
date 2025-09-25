using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories;

namespace Donutsbox.Api.Services;

public class UserSubscriptionService(IEntityRepository<UserSubscription, Guid> repository, IMapper mapper) : IEntityService<UserSubscriptionDto, Guid>
{
    public async Task<UserSubscriptionDto?> AddAsync(UserSubscriptionDto entity)
    {
        var userSubscription = mapper.Map<UserSubscription>(entity);
        var addedSubscription = await repository.AddAsync(userSubscription);
        return mapper.Map<UserSubscriptionDto>(addedSubscription);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<UserSubscriptionDto>> GetAllAsync()
    {
        var userSubscriptions = await repository.GetAllAsync();
        return userSubscriptions.Select(mapper.Map<UserSubscriptionDto>);
    }

    public async Task<UserSubscriptionDto?> GetByIdAsync(Guid id)
    {
        var userSubscription = await repository.GetByIdAsync(id);
        return mapper.Map<UserSubscriptionDto>(userSubscription);
    }

    public async Task<bool> UpdateAsync(UserSubscriptionDto entity, Guid id)
    {
        var updatedUserSubscription = mapper.Map<UserSubscription>(entity);
        return await repository.UpdateAsync(updatedUserSubscription, id);
    }
}
