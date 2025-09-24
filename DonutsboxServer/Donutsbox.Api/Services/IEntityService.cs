namespace Donutsbox.Api.Services;

/// <summary>
/// Интерфейс для сервисов
/// </summary>
public interface IEntityService<DTO, TId>
{
    public Task<IEnumerable<DTO>> GetAllAsync();
    public Task<DTO?> GetByIdAsync(TId id);
    public Task<DTO?> AddAsync(DTO entity);
    public Task<bool> UpdateAsync(DTO entity, TId id);
    public Task<bool> DeleteAsync(TId id);
}
