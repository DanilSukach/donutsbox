using Donutsbox.Domain.Entities;

namespace Donutsbox.Domain.Repositories.AuthorRepository;

public interface IAuthorRepository
{
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize, string? sortBy = null, bool descending = false);
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetTopBySubscribersAsync(int count);
}
