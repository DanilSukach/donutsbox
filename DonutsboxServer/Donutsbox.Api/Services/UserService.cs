using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories;

namespace Donutsbox.Api.Services;

public class UserService(IEntityRepository<User, Guid> repository, IMapper mapper) : IEntityService<UserDto, Guid>
{
    public async Task<UserDto?> AddAsync(UserDto entity)
    {
        var user = mapper.Map<User>(entity);
        var addedUser = await repository.AddAsync(user);
        return mapper.Map<UserDto>(addedUser);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await repository.GetByIdAsync(id);
        if (user == null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await repository.GetAllAsync();
        return users.Select(mapper.Map<UserDto>);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await repository.GetByIdAsync(id);
        return mapper.Map<UserDto>(user);
    }

    public async Task<bool> UpdateAsync(UserDto entity, Guid id)
    {
        var existingUser = await repository.GetByIdAsync(id);
        if (existingUser == null) return false;

        var updatedUser = mapper.Map<User>(entity);
        updatedUser.GUID = id;

        return await repository.UpdateAsync(updatedUser, id);
    }
}
