using Donutsbox.Domain.Entities;

namespace Donutsbox.Domain.Repositories;

public interface IProfileRepository
{
    Task<User?> GetUserDataByIdAsync(Guid id);
}
