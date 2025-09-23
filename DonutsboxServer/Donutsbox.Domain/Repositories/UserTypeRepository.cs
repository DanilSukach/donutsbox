using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;


public class UserTypeRepository(DonutsboxDbContext context) : IEntityRepository<UserType, int>
{
    public async Task<UserType> AddAsync(UserType entity)
    {
        context.UserTypes.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var oldValue = await GetByIdAsync(id);
        if(oldValue == null)
        {
            return false;
        }
        context.UserTypes.Remove(oldValue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UserType>> GetAllAsync() => await context.UserTypes.ToListAsync();

    public async Task<UserType?> GetByIdAsync(int id) => await context.UserTypes.FirstOrDefaultAsync(ut => ut.Id == id);

    public async Task<bool> UpdateAsync(UserType entity, int id)
    {
        var oldValue = await GetByIdAsync(id);
        if(oldValue == null)
        {
            return false;
        }
        oldValue.Name = entity.Name;
        await context.SaveChangesAsync();
        return true;
    }
}

