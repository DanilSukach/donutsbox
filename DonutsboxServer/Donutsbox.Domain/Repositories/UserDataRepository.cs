using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;
public class UserDataRepository(DonutsboxDbContext context) : IEntityRepository<UserData, Guid>
{
    public async Task<UserData> AddAsync(UserData entity)
    {
        context.UsersData.Add(entity);
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
        context.UsersData.Remove(oldValue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UserData>> GetAllAsync() => await context.UsersData.ToListAsync();

    public async Task<UserData?> GetByIdAsync(Guid id) => await context.UsersData.FirstOrDefaultAsync(ud => ud.Id == id);

    public async Task<bool> UpdateAsync(UserData entity, Guid id)
    {
        var oldValue = await GetByIdAsync(id);
        if (oldValue == null)
        {
            return false;
        }
        oldValue.UserId = entity.UserId;
        oldValue.AvatarUrl = entity.AvatarUrl;
        oldValue.Description = entity.Description;
        oldValue.NotificationEmail = entity.NotificationEmail;
        oldValue.PhoneNumber = entity.PhoneNumber;
        oldValue.PaymentInfo = entity.PaymentInfo;
        await context.SaveChangesAsync();
        return true;
    }
}


