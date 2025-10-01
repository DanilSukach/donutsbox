using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.EntityRepository;

public class UserRepository(DonutsboxDbContext context) : IEntityRepository<User, Guid>
{
    public async Task<User> AddAsync(User entity)
    {
        context.Users.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        context.Users.Remove(oldValue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<User>> GetAllAsync() => await context.Users.ToListAsync();

    public async Task<User?> GetByIdAsync(Guid id) => await context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<bool> UpdateAsync(User entity, Guid id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        oldValue.Name = entity.Name;
        //oldValue.TypeId = entity.TypeId;
        //oldValue.AuthId = entity.AuthId;
        await context.SaveChangesAsync();
        return true;
    }
}

