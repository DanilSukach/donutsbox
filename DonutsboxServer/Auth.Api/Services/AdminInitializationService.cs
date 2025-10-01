using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthRepository;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Services;

public class AdminInitializationService(IAuthRepository repository, IConfiguration configuration) : IAdminInitializationService
{
    public async Task InitializeAdminAsync()
    {
        var adminEmail = configuration["Admin:Email"] ?? "admin@donutsbox.com";
        var adminPassword = configuration["Admin:Password"] ?? "Admin123!";

        var emailExists = await repository.EmailExistsAsync(adminEmail);
        if (emailExists)
        {
            return;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

        var user = new UserAuth
        {
            Id = Guid.NewGuid(),
            Password = hash,
            AuthEmail = adminEmail,
        };

        await repository.AddAsync(user, "Administrator");
    }
}
