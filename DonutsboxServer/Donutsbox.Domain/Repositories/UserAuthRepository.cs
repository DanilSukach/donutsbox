using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class UserAuthRepository(DonutsboxDbContext context) : IEntityRepository<UserAuth, string>
{
    public async Task<UserAuth> AddAsync(UserAuth entity)
    {
        context.UsersAuths.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        context.UsersAuths.Remove(oldValue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UserAuth>> GetAllAsync() => await context.UsersAuths.ToListAsync();

    public async Task<UserAuth?> GetByIdAsync(string id) => await context.UsersAuths.FirstOrDefaultAsync(ua => ua.Id == id);

    public async Task<bool> UpdateAsync(UserAuth entity, string id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        oldValue.Password = entity.Password;
        oldValue.AuthEmail = entity.AuthEmail;
        oldValue.LastAuth = entity.LastAuth;
        await context.SaveChangesAsync();
        return true;
    }
}


