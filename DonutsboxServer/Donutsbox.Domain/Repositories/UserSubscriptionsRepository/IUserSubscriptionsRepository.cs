using Donutsbox.Domain.Entities;

namespace Donutsbox.Domain.Repositories.UserSubscriptionsRepository;

public interface IUserSubscriptionsRepository
{
    public Task<User?> GetByIdUserWithSubscriptionsAsync(Guid id);
}
