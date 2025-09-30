using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class AuthRepository(DonutsboxDbContext db) : IAuthRepository
{
    public async Task<(UserAuth?, string?)> GetByEmailAsync(string email)
    {
        var userAuth = await db.UsersAuths.FirstOrDefaultAsync(ua => ua.AuthEmail == email);
        if (userAuth != null) {
            var userWithRole = await db.Users.FirstOrDefaultAsync(u => u.AuthId == userAuth!.Id);
            var roleName = await db.UserTypes.FirstOrDefaultAsync(u => u.Id == userWithRole!.TypeId);
            return (userAuth, roleName!.Name);
        }
        return (null, null);
    }

    public async Task<(UserAuth?, string?)> GetByRefreshTokenAsync(string refreshToken)
    {
        var user = await db.UsersAuths
            .FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow
            );
        if (user != null)
        {
            var userWithRole = await db.Users.FirstOrDefaultAsync(u => u.AuthId == user!.Id);
            var roleName = await db.UserTypes.FirstOrDefaultAsync(u => u.Id == userWithRole!.TypeId);
            return (user, roleName!.Name);
        }
        return (null, null);
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
            GUID = Guid.NewGuid(),
            UserAuth = userAuth,
            Name = userAuth.AuthEmail, 
            TypeId = userType.Id,      
            AuthId = userAuth.Id      
        };

        db.UsersAuths.Add(userAuth);
        db.Users.Add(user);

        await db.SaveChangesAsync();
    }


    public async Task UpdateAsync(UserAuth user)
    {
        db.UsersAuths.Update(user);
        await db.SaveChangesAsync();
    }
}
