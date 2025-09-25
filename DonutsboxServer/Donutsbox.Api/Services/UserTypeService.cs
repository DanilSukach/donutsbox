using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories;

namespace Donutsbox.Api.Services;

public class UserTypeService(IEntityRepository<UserType, int> repository, IMapper mapper) : IEntityService<UserTypeDto, int>
{
    public async Task<UserTypeDto?> AddAsync(UserTypeDto entity)
    {
        var type = mapper.Map<UserType>(entity);
        var addedType = await repository.AddAsync(type);
        return mapper.Map<UserTypeDto>(addedType);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<UserTypeDto>> GetAllAsync()
    {
        var types = await repository.GetAllAsync();
        return types.Select(mapper.Map<UserTypeDto>);
    }

    public async Task<UserTypeDto?> GetByIdAsync(int id)
    {
        var type = await repository.GetByIdAsync(id);
        return mapper.Map<UserTypeDto>(type);
    }

    public async Task<bool> UpdateAsync(UserTypeDto entity, int id)
    {
        var updatedType = mapper.Map<UserType>(entity);
        return await repository.UpdateAsync(updatedType, id);
    }
}
