using Donutsbox.Domain.Entities;

namespace Donutsbox.Domain.Repositories;

public interface IAuthRepository
{
    Task<(UserAuth?, string?)> GetByEmailAsync(string email);
    Task<(UserAuth?, string?)> GetByRefreshTokenAsync(string refreshToken);
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(UserAuth user, string role);
    Task UpdateAsync(UserAuth user);
}
