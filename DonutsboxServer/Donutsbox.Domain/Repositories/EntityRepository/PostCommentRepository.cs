using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.EntityRepository;

public class PostCommentRepository(DonutsboxDbContext context) : IEntityRepository<PostComment, Guid>
{
    public async Task<PostComment> AddAsync(PostComment entity)
    {
        var comment = await context.PostComments.AddAsync(entity);
        await context.SaveChangesAsync();
        return comment.Entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var comment = await GetByIdAsync(id);
        if (comment == null) return false;
        context.PostComments.Remove(comment);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PostComment>> GetAllAsync()
    {
        return await context.PostComments.ToListAsync();
    }

    public async Task<PostComment?> GetByIdAsync(Guid id)
    {
        return await context.PostComments
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> UpdateAsync(PostComment entity, Guid id)
    {
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return true;
    }
}
