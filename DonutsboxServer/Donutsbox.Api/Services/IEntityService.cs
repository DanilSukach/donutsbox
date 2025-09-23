namespace Donutsbox.Api.Services;

/// <summary>
/// Интерфейс для сервисов
/// </summary>
public interface IEntityService<DTO, TId>
{
    public Task<IEnumerable<DTO>> GetAllAsync();
    public Task<DTO?> GetById(TId id);
    public Task<DTO?> Add(DTO entity);
    public Task<bool> Update(DTO entity, TId id);
    public Task<bool> Delete(TId id);
}
