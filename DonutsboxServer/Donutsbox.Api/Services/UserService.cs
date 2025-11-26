using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Donutsbox.Api.Services;

public class UserService(IEntityRepository<User, Guid> repository, IMapper mapper, DonutsboxDbContext db) : IUserService
{
    public async Task<UserDto?> AddAsync(UserDto entity)
    {
        var user = mapper.Map<User>(entity);
        var addedUser = await repository.AddAsync(user);
        return mapper.Map<UserDto>(addedUser);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await repository.DeleteAsync(id);
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
        var updatedUser = mapper.Map<User>(entity);
        return await repository.UpdateAsync(updatedUser, id);
    }

    public async Task<bool> ChangeUserName(UserNameDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);

        var userEntity = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (userEntity == null)
            throw new InvalidOperationException("User not found");

        // Проверка на то же имя
        if (userEntity.Name == dto.Name)
            throw new InvalidOperationException("Новое имя совпадает с текущим");

        // Проверка уникальности имени
        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Name == dto.Name && u.Id != userId);
        if (existingUser != null)
            throw new InvalidOperationException("Пользователь с таким именем уже существует");

        // Проверка длины имени
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length < 3 || dto.Name.Length > 50)
            throw new InvalidOperationException("Имя должно быть от 3 до 50 символов");

        userEntity.Name = dto.Name;
        await db.SaveChangesAsync();
        return true;
    }
}
