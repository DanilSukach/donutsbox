using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthorRepository;
using Donutsbox.Domain.Repositories.EntityRepository;
using Donutsbox.Domain.Context;
using System.Security.Claims;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Api.Services.AuthorService;

public class AuthorService(
    IAuthorRepository authorRepository, 
    IEntityRepository<User, Guid> userRepository, 
    IEntityRepository<UserData, Guid> userDataRepository,
    IEntityRepository<CreatorPageData, Guid> creatorRepository, 
    IEntityRepository<Subscription, Guid> subcriptionRepository, 
    IEntityRepository<SubscriptionPeriod, int> subscriptionPeriodRepository,
    DonutsboxDbContext db) : IAuthorService
{
    public async Task<CreatorPageDataDto> AddCreatorPageAsync(CreatorPageDataDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var author = await userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        if (!string.IsNullOrWhiteSpace(dto.AvatarUrl) && author.UserData != null)
        {
            author.UserData.AvatarUrl = dto.AvatarUrl;
            await userDataRepository.UpdateAsync(author.UserData, author.UserData.Id);
        }

        var entity = new CreatorPageData
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PageName = dto.PageName,
            BannerURL = dto.BannerUrl,
            Description = dto.Description,
            SubscribersCount = dto.SubscribersCount,
            User = author
        };

        var creator = await creatorRepository.AddAsync(entity);

        return new CreatorPageDataDto
        {
            PageName = creator.PageName,
            AvatarUrl = author.UserData?.AvatarUrl,
            BannerUrl = creator.BannerURL,
            Description = creator.Description,
            SubscribersCount = creator.SubscribersCount
        };
    }

    public async Task<SubscriptionDto> AddSubscriptionAsync(SubscriptionCreateDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var author = await authorRepository.GetByIdAsync(userId);

        var periods = await subscriptionPeriodRepository.GetAllAsync();
        Subscription? monthlySub = null;
        foreach (var period in periods)
        {
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                CreatorPageData = author!.CreatorPageData!,
                CreatorPageDataId = author!.CreatorPageData!.Id,
                Price = CalculatePrice(period.Months, dto.Price),
                Name = dto.Name,
                Description = dto.Description,
                PictureURL = dto.PictureURL,
                SubscriptionPeriodId = period.Id,
                SubscriptionPeriod = period
            };
            await subcriptionRepository.AddAsync(subscription);
            if (period.Months == 1)
            {
                monthlySub = subscription;
            }
        }

        if (monthlySub == null)
            throw new InvalidOperationException("Monthly subscription period not found");
        return new SubscriptionDto
        {
            Id = monthlySub.Id,
            Price = monthlySub.Price,
            Name = monthlySub.Name,
            Description = monthlySub.Description,
            PictureURL = monthlySub.PictureURL,
            SubscriptionPeriodId = monthlySub.SubscriptionPeriodId,
            SubscriptionPeriodMonths = monthlySub.SubscriptionPeriod.Months,
            MonthlyPrice = dto.Price,
            ParentSubscriptionId = monthlySub.ParentSubscriptionId
        };
    }

    public async Task<bool> ChangeAuthorName(AuthorNameDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        
        var userEntity = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (userEntity == null)
            throw new InvalidOperationException("User not found");
        
        if (!string.Equals(userEntity.UserType.Name, "Creator", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only creators can update page name");
        
        if (userEntity.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");

        if (userEntity.CreatorPageData.PageName == dto.Name)
            throw new InvalidOperationException("Новое название совпадает с текущим");

        // Проверяем уникальность названия
        var nameExists = await db.CreatorsPageData
            .AnyAsync(c => c.PageName == dto.Name && c.UserId != userId);
        if (nameExists)
            throw new InvalidOperationException("Это название уже занято");

        userEntity.CreatorPageData.PageName = dto.Name;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeAuthorDescription(AuthorDescriptionDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        
        var userEntity = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (userEntity == null)
            throw new InvalidOperationException("User not found");
        
        if (!string.Equals(userEntity.UserType.Name, "Creator", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only creators can update description");
        
        if (userEntity.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found");
            
        if (userEntity.CreatorPageData.Description == dto.Description)
            throw new InvalidOperationException("Новое описание совпадает с текущим");
            
        userEntity.CreatorPageData.Description = dto.Description;
        await db.SaveChangesAsync();
        return true;
    }

    public string CalculatePrice(int periodInMonths, string monthlyPrice)
    {
        var normalizedPrice = monthlyPrice.Replace(',', '.');
        if (decimal.TryParse(normalizedPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceDecimal))
        {
            var totalPrice = priceDecimal * periodInMonths;
            return totalPrice.ToString("F2", CultureInfo.InvariantCulture);
        }
        throw new ArgumentException("Invalid monthly price format");
    }

    public async Task<IEnumerable<AuthorRequestDto>> GetAuthorsAsync(int page, int pageSize, string? sortBy = null, bool descending = false)
    {
        var users = await authorRepository.GetAllAsync(page, pageSize, sortBy, descending);

        var dtos = new List<AuthorRequestDto>();

        foreach (var user in users)
        {
            if (user.CreatorPageData != null && !user.CreatorPageData.IsShadowBanned)
            {
                dtos.Add(new AuthorRequestDto
                {
                    Id = user.Id,
                    PageName = user.CreatorPageData.PageName,
                    AvatarUrl = user.UserData?.AvatarUrl,
                    BannerUrl = user.CreatorPageData.BannerURL,
                    Description = user.CreatorPageData.Description,
                    SubscribersCount = user.CreatorPageData.SubscribersCount,
                    Subscriptions = [.. user.CreatorPageData.Subscriptions.Select(MapSubscription)],
                    IsShadowBanned = user.CreatorPageData.IsShadowBanned
                });
            }
        }

        return dtos;
    }

    public async Task<IEnumerable<AuthorRequestDto>> GetAuthorsAsync()
    {
        var users = await authorRepository.GetAllAsync();

        var dtos = new List<AuthorRequestDto>();

        foreach (var user in users)
        {
            if (user.CreatorPageData != null && !user.CreatorPageData.IsShadowBanned)
            {
                dtos.Add(new AuthorRequestDto
                {
                    Id = user.Id,
                    PageName = user.CreatorPageData.PageName,
                    AvatarUrl = user.UserData?.AvatarUrl,
                    BannerUrl = user.CreatorPageData.BannerURL,
                    Description = user.CreatorPageData.Description,
                    SubscribersCount = user.CreatorPageData.SubscribersCount,
                    Subscriptions = [.. user.CreatorPageData.Subscriptions.Select(MapSubscription)],
                    IsShadowBanned = user.CreatorPageData.IsShadowBanned
                });
            }
        }

        return dtos;
    }

    public async Task<AuthorRequestDto?> GetAuthorByIdAsync(Guid id, Guid? requestingUserId = null)
    {
        var user = await authorRepository.GetByIdAsync(id);

        if (user?.CreatorPageData == null)
            return null;

        // Если автор в теневом бане и запрашивающий пользователь не является владельцем - возвращаем null
        if (user.CreatorPageData.IsShadowBanned && requestingUserId != user.Id)
            return null;

        return new AuthorRequestDto
        {
            Id = user.Id,
            PageName = user.CreatorPageData.PageName,
            AvatarUrl = user.UserData?.AvatarUrl,
            BannerUrl = user.CreatorPageData.BannerURL,
            Description = user.CreatorPageData.Description,
            SubscribersCount = user.CreatorPageData.SubscribersCount,
            Subscriptions = [.. user.CreatorPageData.Subscriptions.Select(MapSubscription)],
            IsShadowBanned = user.CreatorPageData.IsShadowBanned
        };
    }

    public async Task<IEnumerable<AuthorRequestDto>> GetTopAuthorsAsync(int count)
    {
        var users = await authorRepository.GetTopBySubscribersAsync(count);

        var dtos = new List<AuthorRequestDto>();

        foreach (var user in users)
        {
            if (user.CreatorPageData != null)
            {
                dtos.Add(new AuthorRequestDto
                {
                    Id = user.Id,
                    PageName = user.CreatorPageData.PageName,
                    AvatarUrl = user.UserData?.AvatarUrl,
                    BannerUrl = user.CreatorPageData.BannerURL,
                    Description = user.CreatorPageData.Description,
                    SubscribersCount = user.CreatorPageData.SubscribersCount,
                    Subscriptions = [.. user.CreatorPageData.Subscriptions.Select(MapSubscription)],
                    IsShadowBanned = user.CreatorPageData.IsShadowBanned
                });
            }
        }

        return dtos;
    }

    public async Task<IEnumerable<UserRequestDto>> GetTopSupportedUsersAsync(ClaimsPrincipal author, int count)
    {
        var authorIdClaim = author.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var authorId = Guid.Parse(authorIdClaim.Value);
        var users = await authorRepository.GetTopSupportedUsersAsync(authorId, count);

        var dtos = new List<UserRequestDto>();

        foreach (var user in users)
        {
            dtos.Add(new UserRequestDto
            {
                Id = user.Id,
                UserName = user.Name,
                AvatarUrl = user.UserData?.AvatarUrl
            });
        }

        return dtos;
    }

    private static SubscriptionDto MapSubscription(Subscription subscription)
    {
        var months = subscription.SubscriptionPeriod?.Months ?? 1;
        return new SubscriptionDto
        {
            Id = subscription.Id,
            Price = subscription.Price,
            PictureURL = subscription.PictureURL,
            Description = subscription.Description,
            Name = subscription.Name,
            SubscriptionPeriodId = subscription.SubscriptionPeriodId,
            SubscriptionPeriodMonths = months,
            MonthlyPrice = CalculateMonthlyPrice(subscription.Price, months),
            ParentSubscriptionId = subscription.ParentSubscriptionId
        };
    }

    private static string CalculateMonthlyPrice(string price, int months)
    {
        if (months <= 0)
        {
            return price;
        }

        if (decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var totalPrice))
        {
            var monthly = totalPrice / months;
            return monthly.ToString("F2", CultureInfo.InvariantCulture);
        }

        return price;
    }

    public async Task<bool> UpdateBannerAsync(string bannerKey, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        
        var userEntity = await db.Users
            .Include(u => u.UserType)
            .Include(u => u.CreatorPageData)
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new InvalidOperationException("User not found");
        if (!string.Equals(userEntity.UserType.Name, "Creator", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only creators can update banners");
        
        if (userEntity.CreatorPageData == null)
            throw new InvalidOperationException("Creator page not found. You must create a creator page first.");
        
        userEntity.CreatorPageData.BannerURL = bannerKey;
        await db.SaveChangesAsync();
        
        return true;
    }
}
