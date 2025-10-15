using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Repositories.UserSubscriptionsRepository;

public class UserSubscriptionsRepository(DonutsboxDbContext context) : IUserSubscriptionsRepository<User, Guid>
{
    public async Task<User?> GetByIdUserWithSubscriptionsAsync(Guid id) => await context.Users.Include(u => u.UserSubscriptions).ThenInclude(us => us.Subscription).ThenInclude(s => s.CreatorPageData).FirstOrDefaultAsync(u => u.Id == id);
}
