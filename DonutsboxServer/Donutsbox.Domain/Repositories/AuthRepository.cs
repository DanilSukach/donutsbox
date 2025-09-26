using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class AuthRepository(DonutsboxDbContext db) : IAuthRepository
{
    public async Task<UserAuth?> GetByEmailAsync(string email)
    {
        return await db.UsersAuths.FirstOrDefaultAsync(u => u.AuthEmail == email);
    }

    public async Task<UserAuth?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await db.UsersAuths
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiryTime > DateTime.UtcNow);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await db.UsersAuths.AnyAsync(u => u.AuthEmail == email);
    }

    public async Task AddAsync(UserAuth user)
    {
        db.UsersAuths.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserAuth user)
    {
        db.UsersAuths.Update(user);
        await db.SaveChangesAsync();
    }
}
