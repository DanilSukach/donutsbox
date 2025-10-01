namespace Donutsbox.Domain.Repositories.EntityRepository;

public interface IEntityRepository<T, TId>
{
    public Task<IEnumerable<T>> GetAllAsync();
    public Task<T?> GetByIdAsync(TId id);
    public Task<T> AddAsync(T entity);
    public Task<bool> UpdateAsync(T entity, TId id);
    public Task<bool> DeleteAsync(TId id);
}
