using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class AuthRepository(DonutsboxDbContext db) : IAuthRepository
{
    public async Task<UserAuth?> GetByEmailAsync(string email)
    {
        var userAuth = await db.UsersAuths
            .Include(u => u.User)
                .ThenInclude(u => u!.UserType)
            .FirstOrDefaultAsync(ua => ua.AuthEmail == email);
        return userAuth;
    }

    public async Task<UserAuth?> GetByRefreshTokenAsync(string refreshToken)
    {
        var userAuth = await db.UsersAuths
            .Include(u => u.User)
                .ThenInclude(u => u!.UserType)
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

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserAuth = userAuth,
            UserAuthId = userAuth.Id,
            Name = userAuth.AuthEmail,
            UserType = userType,
            UserTypeId = userType.Id,
        };

        db.Users.Add(user);

        await db.SaveChangesAsync();
    }


    public async Task UpdateAsync(UserAuth user)
    {
        db.UsersAuths.Update(user);
        await db.SaveChangesAsync();
    }
}
