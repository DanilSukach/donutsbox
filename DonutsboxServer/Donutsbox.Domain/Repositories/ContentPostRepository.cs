using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class ContentPostRepository(DonutsboxDbContext context) : IEntityRepository<ContentPost, string>
{
    public async Task<ContentPost> AddAsync(ContentPost entity)
    {
        var post = await context.ContentPosts.AddAsync(entity);
        await context.SaveChangesAsync();
        return post.Entity;
    }

    public async Task<bool> DeleteAsync(string id)
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

    public async Task<ContentPost?> GetByIdAsync(string id)
    {
        return await context.ContentPosts
                            .FirstOrDefaultAsync(c => c.PostId == id);
    }

    public async Task<bool> UpdateAsync(ContentPost entity, string id)
    {
        var post = await GetByIdAsync(id);
        if (post == null) return false;
        post.Title = entity.Title;
        post.Text = entity.Text;
        post.AudioURLs = entity.AudioURLs;
        post.PictureURLs = entity.PictureURLs;
        post.VideoURLs = entity.VideoURLs;
        post.DislikesCount = entity.DislikesCount;
        post.CreatedAt = entity.CreatedAt;
        post.PageId = entity.PageId;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDislikesAsync(string id, int dislikesCount)
    {
        var post = await GetByIdAsync(id);
        if (post == null) return false;
        post.DislikesCount = dislikesCount;
        await context.SaveChangesAsync();
        return true;
    }
}
