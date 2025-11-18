using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.EntityRepository;

public class PostReactionRepository(DonutsboxDbContext context) : IEntityRepository<PostReaction, Guid>
{
    public async Task<PostReaction> AddAsync(PostReaction entity)
    {
        var reaction = await context.PostReactions.AddAsync(entity);
        await context.SaveChangesAsync();
        return reaction.Entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var reaction = await GetByIdAsync(id);
        if (reaction == null) return false;
        context.PostReactions.Remove(reaction);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PostReaction>> GetAllAsync()
    {
        return await context.PostReactions.ToListAsync();
    }

    public async Task<PostReaction?> GetByIdAsync(Guid id)
    {
        return await context.PostReactions
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> UpdateAsync(PostReaction entity, Guid id)
    {
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return true;
    }
}
