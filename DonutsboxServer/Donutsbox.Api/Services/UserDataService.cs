using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;

namespace Donutsbox.Api.Services;

public class UserDataService(IEntityRepository<UserData, Guid> repository, IMapper mapper) : IEntityService<UserDataDto, Guid>
{
    public async Task<UserDataDto?> AddAsync(UserDataDto entity)
    {
        var userData = mapper.Map<UserData>(entity);
        var addedUserData = await repository.AddAsync(userData);
        return mapper.Map<UserDataDto>(addedUserData);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<UserDataDto>> GetAllAsync()
    {
        var userData = await repository.GetAllAsync();
        return userData.Select(mapper.Map<UserDataDto>);
    }

    public async Task<UserDataDto?> GetByIdAsync(Guid id)
    {
        var userData = await repository.GetByIdAsync(id);
        return mapper.Map<UserDataDto>(userData);
    }

    public async Task<bool> UpdateAsync(UserDataDto entity, Guid id)
    {
        var updatedUserData = mapper.Map<UserData>(entity);
        return await repository.UpdateAsync(updatedUserData, id);
    }
}
