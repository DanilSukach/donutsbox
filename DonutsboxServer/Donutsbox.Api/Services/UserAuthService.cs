using Donutsbox.Api.Dto;
using Donutsbox.Domain.Repositories;
using Donutsbox.Domain.Entities;
using AutoMapper;

namespace Donutsbox.Api.Services;

public class UserAuthService(IEntityRepository<UserAuth, Guid> repository, IMapper mapper) : IEntityService<UserAuthDto, Guid>
{
    public async Task<UserAuthDto?> AddAsync(UserAuthDto entity)
    {
        var user = mapper.Map<UserAuth>(entity);
        var addedUser = await repository.AddAsync(user);
        return mapper.Map<UserAuthDto>(addedUser);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await repository.GetByIdAsync(id);
        if (user == null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<IEnumerable<UserAuthDto>> GetAllAsync()
    {
        var users = await repository.GetAllAsync();
        return users.Select(mapper.Map<UserAuthDto>);
    }

    public async Task<UserAuthDto?> GetByIdAsync(Guid id)
    {
        var user = await repository.GetByIdAsync(id);
        return mapper.Map<UserAuthDto>(user);
    }

    public async Task<bool> UpdateAsync(UserAuthDto entity, Guid id)
    {
        var existingUser = await repository.GetByIdAsync(id);
        if (existingUser == null) return false;

        var updatedUser = mapper.Map<UserAuth>(entity);
        updatedUser.Id = id;

        return await repository.UpdateAsync(updatedUser, id);
    }
}