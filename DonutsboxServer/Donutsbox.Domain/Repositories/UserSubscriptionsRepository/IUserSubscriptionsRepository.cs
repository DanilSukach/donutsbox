using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;


namespace Donutsbox.Domain.Repositories.UserSubscriptionsRepository;

public interface IUserSubscriptionsRepository<T, Guid> : IEntityRepository<T, Guid>
{
    public Task<T?> GetByIdUserWithSubscriptionsAsync(Guid id);
}
