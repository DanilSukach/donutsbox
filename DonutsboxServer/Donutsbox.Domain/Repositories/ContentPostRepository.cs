using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class ContentPostRepository(DonutsboxDbContext context) : IEntityRepository<ContentPost, Guid>
{
    public async Task<ContentPost> AddAsync(ContentPost entity)
    {
        var post = await context.ContentPosts.AddAsync(entity);
        await context.SaveChangesAsync();
        return post.Entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var post = await GetByIdAsync(id);
        if (post == null) return false;
        context.ContentPosts.Remove(post);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ContentPost>> GetAllAsync()
    {
        return await context.ContentPosts.ToListAsync();
    }

    public async Task<ContentPost?> GetByIdAsync(Guid id)
    {
        return await context.ContentPosts
                            .FirstOrDefaultAsync(c => c.PostId == id);
    }

    public async Task<bool> UpdateAsync(ContentPost entity, Guid id)
    {
        var post = await GetByIdAsync(id);
        if (post == null) return false;
        post.PageId = entity.PageId;
        post.Title = entity.Title;
        post.Text = entity.Text;
        post.CreatedAt = entity.CreatedAt;
        post.DislikesCount = entity.DislikesCount;
        post.AudioURLs = entity.AudioURLs;
        post.PictureURLs = entity.PictureURLs;
        post.VideoURLs = entity.VideoURLs;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDislikesAsync(Guid id, int dislikesCount)
    {
        var post = await GetByIdAsync(id);
        if (post == null) return false;
        post.DislikesCount = dislikesCount;
        await context.SaveChangesAsync();
        return true;
    }
}
