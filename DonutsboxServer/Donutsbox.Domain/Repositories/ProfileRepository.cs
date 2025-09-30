using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class ProfileRepository(DonutsboxDbContext context) : IProfileRepository
{
    public async Task<User?> GetUserDataByIdAsync(Guid id)
    {
        var result = await context.Users.Include(u => u.UserData).FirstOrDefaultAsync(u => u.Id == id);
        return result;
    }
}