using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories;

public class ProfileRepository(DonutsboxDbContext context) : IProfileRepository<UserData, User>
{
    public async Task<(UserData?, User?)> GetUserDataByIdAsync(Guid id)
    {
        var result = await context.UsersData.Where(ud => ud.Id == id)
                            .Join(context.Users,
                            ud => ud.UserId,
                            u => u.GUID,
                            (ud, u) => new { UserData = ud, User = u })
                            .FirstOrDefaultAsync();
        if (result == null)
        {
            return (null, null!);
        }
        return (result.UserData, result.User);
    }
}