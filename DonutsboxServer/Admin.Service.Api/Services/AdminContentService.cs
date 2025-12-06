using Admin.Service.Api.Dto;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;
using Microsoft.EntityFrameworkCore;

namespace Admin.Service.Api.Services;

/// <summary>
/// Сервис для администрирования контента
/// </summary>
public class AdminContentService(DonutsboxDbContext context,
        ILogger<AdminContentService> logger) : IAdminContentService
{

    public async Task<IEnumerable<AdminContentPostListDto>> GetAllPostsAsync()
    {
        var posts = await context.ContentPosts
            .Include(p => p.CreatorPageData)
                .ThenInclude(c => c.User)
            .Include(p => p.PostComments)
            .Include(p => p.PostReactions)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(p => new AdminContentPostListDto
        {
            Id = p.Id,
            Title = p.Title ?? string.Empty,
            Text = p.Text ?? string.Empty,
            CreatorPageDataId = p.CreatorPageDataId,
            CreatorName = p.CreatorPageData?.User?.Name ?? "Unknown",
            IsPublished = p.IsPublished,
            CreatedAt = p.CreatedAt,
            LikesCount = p.LikesCount,
            DislikesCount = p.DislikesCount,
            CommentsCount = p.CommentsCount,
            MediaCount = p.AudioURLs.Count + p.Videos.Count + p.Images.Count,
            IsShadowBanned = p.IsShadowBanned
        });
    }

    public async Task<AdminContentPostListDto?> GetPostByIdAsync(Guid postId)
    {
        var post = await context.ContentPosts
            .Include(p => p.CreatorPageData)
                .ThenInclude(c => c.User)
            .Include(p => p.PostComments)
            .Include(p => p.PostReactions)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null) return null;

        return new AdminContentPostListDto
        {
            Id = post.Id,
            Title = post.Title ?? string.Empty,
            Text = post.Text ?? string.Empty,
            CreatorPageDataId = post.CreatorPageDataId,
            CreatorName = post.CreatorPageData?.User?.Name ?? "Unknown",
            IsPublished = post.IsPublished,
            CreatedAt = post.CreatedAt,
            LikesCount = post.LikesCount,
            DislikesCount = post.DislikesCount,
            CommentsCount = post.CommentsCount,
            MediaCount = post.AudioURLs.Count + post.Videos.Count + post.Images.Count,
            IsShadowBanned = post.IsShadowBanned
        };
    }

    public async Task<AdminDeleteResultDto> DeletePostAsync(Guid postId)
    {
        var result = new AdminDeleteResultDto
        {
            Success = false,
            DeletedEntities = []
        };

        try
        {
            var post = await context.ContentPosts
                .Include(p => p.PostComments)
                .Include(p => p.PostReactions)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                result.Message = $"Пост с ID {postId} не найден";
                return result;
            }

            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Удаляем комментарии
                var commentsCount = post.PostComments.Count;
                context.PostComments.RemoveRange(post.PostComments);
                result.DeletedEntities.Add($"Комментарии: {commentsCount} шт.");

                // Удаляем реакции
                var reactionsCount = post.PostReactions.Count;
                context.PostReactions.RemoveRange(post.PostReactions);
                result.DeletedEntities.Add($"Реакции: {reactionsCount} шт.");

                // Удаляем сам пост
                context.ContentPosts.Remove(post);
                result.DeletedEntities.Add("Пост");

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = true;
                result.Message = $"Пост '{post.Title}' успешно удален";

                logger.LogInformation("Пост {PostId} успешно удален", postId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Ошибка при удалении поста {PostId}", postId);
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Message = $"Ошибка при удалении поста: {ex.Message}";
            logger.LogError(ex, "Ошибка при удалении поста {PostId}", postId);
        }

        return result;
    }

    public async Task<AdminDeleteResultDto> DeletePostsAsync(List<Guid> postIds)
    {
        var result = new AdminDeleteResultDto
        {
            Success = true,
            DeletedEntities = [],
            Warnings = []
        };

        var successCount = 0;
        var failCount = 0;

        foreach (var postId in postIds)
        {
            var deleteResult = await DeletePostAsync(postId);
            if (deleteResult.Success)
            {
                successCount++;
                result.DeletedEntities.AddRange(deleteResult.DeletedEntities);
            }
            else
            {
                failCount++;
                result.Warnings.Add($"Post {postId}: {deleteResult.Message}");
            }
        }

        result.Message = $"Успешно удалено: {successCount}, Ошибок: {failCount}";
        result.Success = failCount == 0;

        return result;
    }

    public async Task<AdminDeleteResultDto> DeleteCreatorPostsAsync(Guid creatorPageDataId)
    {
        var result = new AdminDeleteResultDto
        {
            Success = false,
            DeletedEntities = []
        };

        try
        {
            var posts = await context.ContentPosts
                .Where(p => p.CreatorPageDataId == creatorPageDataId)
                .Select(p => p.Id)
                .ToListAsync();

            if (posts.Count == 0)
            {
                result.Message = $"Постов для создателя с ID {creatorPageDataId} не найдено";
                result.Success = true;
                return result;
            }

            var deleteResult = await DeletePostsAsync(posts);
            return deleteResult;
        }
        catch (Exception ex)
        {
            result.Message = $"Ошибка при удалении постов создателя: {ex.Message}";
            logger.LogError(ex, "Ошибка при удалении постов создателя {CreatorPageDataId}", creatorPageDataId);
        }

        return result;
    }

    public async Task<AdminActionResponseDto> ShadowBanPostAsync(Guid postId)
    {
        var result = new AdminActionResponseDto
        {
            Success = false
        };

        try
        {
            var post = await context.ContentPosts
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                result.Message = $"Пост с ID {postId} не найден";
                return result;
            }

            post.IsShadowBanned = true;
            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = "Пост добавлен в теневой бан";
            logger.LogInformation("Пост {PostId} добавлен в теневой бан", postId);
        }
        catch (Exception ex)
        {
            result.Message = $"Ошибка при добавлении поста в теневой бан: {ex.Message}";
            logger.LogError(ex, "Ошибка при добавлении поста {PostId} в теневой бан", postId);
        }

        return result;
    }

    public async Task<AdminActionResponseDto> UnshadowBanPostAsync(Guid postId)
    {
        var result = new AdminActionResponseDto
        {
            Success = false
        };

        try
        {
            var post = await context.ContentPosts
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                result.Message = $"Пост с ID {postId} не найден";
                return result;
            }

            post.IsShadowBanned = false;
            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = "Теневой бан с поста снят";
            logger.LogInformation("Теневой бан с поста {PostId} снят", postId);
        }
        catch (Exception ex)
        {
            result.Message = $"Ошибка при снятии теневого бана с поста: {ex.Message}";
            logger.LogError(ex, "Ошибка при снятии теневого бана с поста {PostId}", postId);
        }

        return result;
    }
}