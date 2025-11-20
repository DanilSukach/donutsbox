using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Donutsbox.Domain.Repositories.EntityRepository;

public class ReactionTypeRepository(DonutsboxDbContext context) : IEntityRepository<ReactionType, int>
{
    public async Task<ReactionType> AddAsync(ReactionType entity)
    {
        context.ReactionTypes.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        context.ReactionTypes.Remove(oldValue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ReactionType>> GetAllAsync() => await context.ReactionTypes.ToListAsync();

    public async Task<ReactionType?> GetByIdAsync(int id) => await context.ReactionTypes.FirstOrDefaultAsync(rt => rt.Id == id);

    public async Task<bool> UpdateAsync(ReactionType entity, int id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        oldValue.Name = entity.Name;
        await context.SaveChangesAsync();
        return true;
    }
}
