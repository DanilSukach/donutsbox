using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthorRepository;
using Donutsbox.Domain.Repositories.EntityRepository;
using System.Security.Claims;

namespace Donutsbox.Api.Services.UserInteractionService;

public class UserInteractionService(
    IEntityRepository<UserSubscription, Guid> userSubscriptionRepository, 
    IEntityRepository<User, Guid> userRepository, 
    IEntityRepository<Subscription, Guid> subscriptionRepository,
    IEntityRepository<ContentPost, Guid> contentPostRepository,
    IEntityRepository<PostReaction, Guid> postReactionRepository,
    IEntityRepository<ReactionType, int> reactionTypeRepository) : IUserInteractionService
{
    public async Task<UserSubscriptionDto> SubscribeUserAsync(UserSubscriptionCreateDto userSubscription, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await userRepository.GetByIdAsync(userId);
        var subscription = await subscriptionRepository.GetByIdAsync(userSubscription.SubscriptionId);

        var userSubscriptionEntity = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionId = userSubscription.SubscriptionId,
            User = userEntity!,
            Subscription = subscription!,
            BeginDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(subscription!.SubscriptionPeriod.Months)
        };
        var result = await userSubscriptionRepository.AddAsync(userSubscriptionEntity);
        return new UserSubscriptionDto
        {
            Id = result.Id,
            UserId = result.UserId,
            SubscriptionId = result.SubscriptionId,
            BeginDate = result.BeginDate,
            EndDate = result.EndDate
        };
    }

    public async Task UnsubscribeUserAsync(Guid creatorUserId, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);

        var creatorUser = await userRepository.GetByIdAsync(creatorUserId) ?? throw new ArgumentException("Creator user not found");

        var userSubscriptions = await userSubscriptionRepository.GetAllAsync();
        var activeSubscriptions = userSubscriptions
            .Where(us => us.UserId == userId &&
                        us.Subscription.CreatorPageData.UserId == creatorUserId)
            .ToList();

        if (activeSubscriptions.Count == 0)
        {
            throw new InvalidOperationException("No active subscription found for this creator");
        }

        foreach (var subscription in activeSubscriptions)
        {
            await userSubscriptionRepository.DeleteAsync(subscription.Id);
        }
    }

    public async Task<PostReactionDto> ChangeReaction(ClaimsPrincipal user, ContentPostReactionDto reaction)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var userEntity = await userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        var contentPost = await contentPostRepository.GetByIdAsync(reaction.PostId) ?? throw new InvalidOperationException("Post not found");
        var newReactionType = await reactionTypeRepository.GetByIdAsync(reaction.ReactionTypeId) ?? throw new InvalidOperationException("Reaction type not found");

        var allReactions = (await postReactionRepository.GetAllAsync()).ToList();
        var existing = allReactions.FirstOrDefault(pr => pr.ContentPostId == reaction.PostId && pr.UserId == userId);

        if (existing != null)
        {
            if (existing.ReactionType.Id == 1) contentPost.LikesCount = Math.Max(0, contentPost.LikesCount - 1);
            else if (existing.ReactionType.Id == 2) contentPost.DislikesCount = Math.Max(0, contentPost.DislikesCount - 1);

            if (existing.ReactionTypeId == reaction.ReactionTypeId) // если та же реакция - удаляем
            {
                // удаляем реакцию и обновляем счётчики
                await postReactionRepository.DeleteAsync(existing.Id);
                contentPost.PostReactions.Remove(existing);
                return new PostReactionDto
                {
                    Id = existing.Id,
                    UserId = existing.UserId,
                    ContentPostId = existing.ContentPostId,
                    ReactionTypeId = 0 // пока под вопросом что возвращать при удалении реакции, оставлю id типа реакции = 0, если удалена
                };
            }

            if (newReactionType.Id == 1) contentPost.LikesCount++;
            else if (newReactionType.Id == 2) contentPost.DislikesCount++;

            existing.ReactionTypeId = reaction.ReactionTypeId;
            existing.ReactionType = newReactionType;
            await postReactionRepository.UpdateAsync(existing, existing.Id);

            await contentPostRepository.UpdateAsync(contentPost, contentPost.Id);

            return new PostReactionDto
            {
                Id = existing.Id,
                UserId = existing.UserId,
                ContentPostId = existing.ContentPostId,
                ReactionTypeId = existing.ReactionTypeId
            };
        }

        var postReactionEntity = new PostReaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = userEntity,
            ContentPostId = reaction.PostId,
            ReactionTypeId = reaction.ReactionTypeId,
            ContentPost = contentPost,
            ReactionType = newReactionType
        };

        var result = await postReactionRepository.AddAsync(postReactionEntity);

        if (newReactionType.Name == "Like") contentPost.LikesCount++;
        else if (newReactionType.Name == "Dislike") contentPost.DislikesCount++;

        await contentPostRepository.UpdateAsync(contentPost, contentPost.Id);
        contentPost.PostReactions.Add(result);

        return new PostReactionDto
        {
            Id = result.Id,
            UserId = result.UserId,
            ContentPostId = result.ContentPostId,
            ReactionTypeId = result.ReactionTypeId
        };
    }
}