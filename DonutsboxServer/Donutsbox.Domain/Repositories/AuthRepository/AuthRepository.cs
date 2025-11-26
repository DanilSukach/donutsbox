using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.AuthRepository;

public class AuthRepository(DonutsboxDbContext db) : IAuthRepository
{
    public async Task<UserAuth?> GetByEmailAsync(string email)
    {
        var userAuth = await db.UsersAuths
            .Include(u => u.User)
                .ThenInclude(u => u!.UserType)
            .Include(u => u.User)
                .ThenInclude(u => u!.CreatorPageData)
            .FirstOrDefaultAsync(ua => ua.AuthEmail == email);
        return userAuth;
    }

    public async Task<UserAuth?> GetByIdAsync(Guid id)
    {
        var userAuth = await db.UsersAuths
            .Include(u => u.User)
                .ThenInclude(u => u!.UserType)
            .FirstOrDefaultAsync(ua => ua.Id == id);
        return userAuth;
    }

    public async Task<UserAuth?> GetByUserIdAsync(Guid userId)
    {
        var userAuth = await db.UsersAuths
            .Include(u => u.User)
                .ThenInclude(u => u!.UserType)
            .FirstOrDefaultAsync(ua => ua.User!.Id == userId);
        return userAuth;
    }

    public async Task<UserAuth?> GetByRefreshTokenAsync(string refreshToken)
    {
        var userAuth = await db.UsersAuths
            .Include(u => u.User)
                .ThenInclude(u => u!.UserType)
            .Include(u => u.User)
                .ThenInclude(u => u!.CreatorPageData)
            .FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow
            );
        return userAuth;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await db.UsersAuths.AnyAsync(u => u.AuthEmail == email);
    }

    public async Task AddAsync(UserAuth userAuth, string roleName)
    {
        var userType = await db.UserTypes
            .FirstOrDefaultAsync(ut => ut.Name == roleName) ?? throw new InvalidOperationException($"Role '{roleName}' not found.");

        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            UserAuth = userAuth,
            UserAuthId = userAuth.Id,
            Name = userAuth.AuthEmail,
            UserType = userType,
            UserTypeId = userType.Id,
        };

        // Создаём UserData для хранения аватарки и других данных пользователя
        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = user,
            AvatarUrl = string.Empty
        };

        user.UserData = userData;

        db.Users.Add(user);

        await db.SaveChangesAsync();
    }


    public async Task UpdateAsync(UserAuth user)
    {
        // Проверяем отслеживается ли уже эта сущность
        var trackedEntity = db.ChangeTracker.Entries<UserAuth>()
            .FirstOrDefault(e => e.Entity.Id == user.Id);

        if (trackedEntity != null)
        {
            // Сущность уже отслеживается - просто сохраняем изменения
            await db.SaveChangesAsync();
        }
        else
        {
            // Сущность не отслеживается - обновляем явно
            db.UsersAuths.Update(user);
            await db.SaveChangesAsync();
        }
    }
}
