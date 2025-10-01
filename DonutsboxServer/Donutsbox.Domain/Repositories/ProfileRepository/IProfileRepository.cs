using Donutsbox.Domain.Entities;

namespace Donutsbox.Domain.Repositories.ProfileRepository;

public interface IProfileRepository
{
    Task<User?> GetUserDataByIdAsync(Guid id);
}
