using Admin.Service.Api.Dto;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;
using Microsoft.EntityFrameworkCore;

namespace Admin.Service.Api.Services;

/// <summary>
/// Сервис для администрирования пользователей
/// </summary>
public class AdminUserService(DonutsboxDbContext context,
        ILogger<AdminUserService> logger) : IAdminUserService
{
    

    public async Task<IEnumerable<AdminUserListDto>> GetAllUsersAsync()
    {
        var users = await context.Users
            .Include(u => u.UserAuth)
            .Include(u => u.UserType)
            .Include(u => u.UserData)
            .Include(u => u.CreatorPageData)
            .Include(u => u.UserSubscriptions)
            .ToListAsync();

        var result = new List<AdminUserListDto>();

        foreach (var user in users)
        {
            var postsCount = 0;
            if (user.CreatorPageData != null)
            {
                postsCount = await context.ContentPosts
                    .CountAsync(p => p.CreatorPageDataId == user.CreatorPageData.Id);
            }

            result.Add(new AdminUserListDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.UserAuth.AuthEmail,
                UserType = user.UserType.Name,
                CreatedAt = (DateTime)user.UserAuth.LastAuth!,
                HasCreatorPage = user.CreatorPageData != null,
                PostsCount = postsCount,
                SubscriptionsCount = user.UserSubscriptions?.Count ?? 0
            });
        }

        return result;
    }

    public async Task<AdminUserListDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await context.Users
            .Include(u => u.UserAuth)
            .Include(u => u.UserType)
            .Include(u => u.UserData)
            .Include(u => u.CreatorPageData)
            .Include(u => u.UserSubscriptions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        var postsCount = 0;
        if (user.CreatorPageData != null)
        {
            postsCount = await context.ContentPosts
                .CountAsync(p => p.CreatorPageDataId == user.CreatorPageData.Id);
        }

        return new AdminUserListDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.UserAuth.AuthEmail,
            UserType = user.UserType.Name,
            CreatedAt = (DateTime)user.UserAuth.LastAuth!,
            HasCreatorPage = user.CreatorPageData != null,
            PostsCount = postsCount,
            SubscriptionsCount = user.UserSubscriptions?.Count ?? 0
        };
    }

    public async Task<AdminDeleteResultDto> DeleteUserAsync(Guid userId)
    {
        var result = new AdminDeleteResultDto
        {
            Success = false,
            DeletedEntities = []
        };

        try
        {
            var user = await context.Users
                .Include(u => u.UserAuth)
                .Include(u => u.UserData)
                .Include(u => u.CreatorPageData)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                result.Message = $"Пользователь с ID {userId} не найден";
                return result;
            }

            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Удаляем посты если пользователь - создатель
                if (user.CreatorPageData != null)
                {
                    var posts = await context.ContentPosts
                        .Where(p => p.CreatorPageDataId == user.CreatorPageData.Id)
                        .ToListAsync();

                    foreach (var post in posts)
                    {
                        // Удаляем комментарии к постам
                        var comments = await context.PostComments
                            .Where(c => c.ContentPostId == post.Id)
                            .ToListAsync();
                        context.PostComments.RemoveRange(comments);
                        result.DeletedEntities.Add($"Комментарии к посту {post.Id}: {comments.Count} шт.");

                        // Удаляем реакции к постам
                        var reactions = await context.PostReactions
                            .Where(r => r.ContentPostId == post.Id)
                            .ToListAsync();
                        context.PostReactions.RemoveRange(reactions);
                        result.DeletedEntities.Add($"Реакции к посту {post.Id}: {reactions.Count} шт.");
                    }

                    context.ContentPosts.RemoveRange(posts);
                    result.DeletedEntities.Add($"Посты: {posts.Count} шт.");

                    // Удаляем страницу создателя
                    context.CreatorsPageData.Remove(user.CreatorPageData);
                    result.DeletedEntities.Add("Страница создателя");
                }

                // Удаляем подписки пользователя
                var userSubscriptions = await context.UsersSubscriptions
                    .Where(s => s.UserId == userId)
                    .ToListAsync();
                context.UsersSubscriptions.RemoveRange(userSubscriptions);
                result.DeletedEntities.Add($"Подписки: {userSubscriptions.Count} шт.");

                // Удаляем комментарии пользователя
                var userComments = await context.PostComments
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                context.PostComments.RemoveRange(userComments);
                result.DeletedEntities.Add($"Комментарии пользователя: {userComments.Count} шт.");

                // Удаляем реакции пользователя
                var userReactions = await context.PostReactions
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                context.PostReactions.RemoveRange(userReactions);
                result.DeletedEntities.Add($"Реакции пользователя: {userReactions.Count} шт.");

                // Удаляем данные пользователя
                if (user.UserData != null)
                {
                    context.UsersData.Remove(user.UserData);
                    result.DeletedEntities.Add("Данные профиля");
                }

                // Удаляем самого пользователя
                context.Users.Remove(user);
                result.DeletedEntities.Add("Пользователь");

                // Удаляем auth данные
                context.UsersAuths.Remove(user.UserAuth);
                result.DeletedEntities.Add("Данные авторизации");

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = true;
                result.Message = $"Пользователь {user.Name} успешно удален";

                logger.LogInformation("Пользователь {UserId} ({UserName}) успешно удален", userId, user.Name);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", userId);
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Message = $"Ошибка при удалении пользователя: {ex.Message}";
            logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", userId);
        }

        return result;
    }

    public async Task<AdminDeleteResultDto> DeleteUsersAsync(List<Guid> userIds)
    {
        var result = new AdminDeleteResultDto
        {
            Success = true,
            DeletedEntities = [],
            Warnings = []
        };

        var successCount = 0;
        var failCount = 0;

        foreach (var userId in userIds)
        {
            var deleteResult = await DeleteUserAsync(userId);
            if (deleteResult.Success)
            {
                successCount++;
                result.DeletedEntities.AddRange(deleteResult.DeletedEntities);
            }
            else
            {
                failCount++;
                result.Warnings.Add($"User {userId}: {deleteResult.Message}");
            }
        }

        result.Message = $"Успешно удалено: {successCount}, Ошибок: {failCount}";
        result.Success = failCount == 0;

        return result;
    }
}
