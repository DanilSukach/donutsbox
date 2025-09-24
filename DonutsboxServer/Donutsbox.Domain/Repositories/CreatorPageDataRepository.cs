using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class CreatorPageDataRepository(DonutsboxDbContext context) : IEntityRepository<CreatorPageData, Guid>
{
    public async Task<CreatorPageData> AddAsync(CreatorPageData entity)
    {
        var pageData = await context.CreatorsPageData.AddAsync(entity);
        await context.SaveChangesAsync();
        return pageData.Entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var pageData = await GetByIdAsync(id);
        if (pageData == null) return false;
        context.CreatorsPageData.Remove(pageData);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CreatorPageData>> GetAllAsync()
    {
        return await context.CreatorsPageData.ToListAsync();
    }

    public async Task<CreatorPageData?> GetByIdAsync(Guid id)
    {
        return await context.CreatorsPageData
                            .FirstOrDefaultAsync(c => c.GUID == id);
    }

    public async Task<bool> UpdateAsync(CreatorPageData entity, Guid id)
    {
        var pageData = await GetByIdAsync(id);
        if (pageData == null) return false;
        pageData.GUID = entity.GUID;
        pageData.PageName = entity.PageName;
        pageData.AvatarURL = entity.AvatarURL;
        pageData.BannerURL = entity.BannerURL;
        pageData.Description = entity.Description;
        pageData.SubscribersCount = entity.SubscribersCount;
        await context.SaveChangesAsync();
        return true;
    }
}
