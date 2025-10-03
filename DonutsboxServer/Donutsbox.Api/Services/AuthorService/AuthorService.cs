using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthorRepository;
using Donutsbox.Domain.Repositories.EntityRepository;
using System.Security.Claims;

namespace Donutsbox.Api.Services.AuthorService;

public class AuthorService(IAuthorRepository authorRepository, IEntityRepository<User, Guid> userRepository, IEntityRepository<CreatorPageData, Guid> creatorRepository, IEntityRepository<Subscription, Guid> subcriptionRepository, IEntityRepository<SubscriptionPeriod, int> subscriptionPeriodRepository) : IAuthorService
{
    public async Task<CreatorPageDataDto> AddCreatorPageAsync(CreatorPageDataDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var author = await userRepository.GetByIdAsync(userId);

        var entity = new CreatorPageData
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PageName = dto.PageName,
            AvatarURL = dto.AvatarUrl,
            BannerURL = dto.BannerUrl,
            Description = dto.Description,
            SubscribersCount = dto.SubscribersCount,
            User = author!
        };

        var creator = await creatorRepository.AddAsync(entity);

        return new CreatorPageDataDto
        {

            PageName = creator.PageName,
            AvatarUrl = creator.AvatarURL,
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
                PictureURL = dto.PictureURL!,
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
        };
    }

    public string CalculatePrice(int periodInMonths, string monthlyPrice)
    {
        if (decimal.TryParse(monthlyPrice, out var priceDecimal))
        {
            var totalPrice = priceDecimal * periodInMonths;
            return totalPrice.ToString("F2");
        }
        throw new ArgumentException("Invalid monthly price format");
    }

    public async Task<IEnumerable<AuthorRequestDto>> GetAuthorsAsync(int page, int pageSize, string? sortBy = null, bool descending = false)
    {
        var users = await authorRepository.GetAllAsync(page, pageSize, sortBy, descending);

        var dtos = new List<AuthorRequestDto>();

        foreach (var user in users)
        {
            if (user.CreatorPageData != null)
            {
                dtos.Add(new AuthorRequestDto
                {
                    Id = user.Id,
                    PageName = user.CreatorPageData.PageName,
                    AvatarUrl = user.CreatorPageData.AvatarURL,
                    Description = user.CreatorPageData.Description,
                    SubscribersCount = user.CreatorPageData.SubscribersCount,
                    Subscriptions = [.. user.CreatorPageData.Subscriptions.Select(s => new SubscriptionDto
                    {
                        Id = s.Id,
                        Price = s.Price,
                        PictureURL = s.PictureURL,
                        Description = s.Description,
                        Name = s.Name,
                    })]
                });
            }
        }

        return dtos;
    }

    public async Task<AuthorRequestDto?> GetAuthorByIdAsync(Guid id)
    {
        var user = await authorRepository.GetByIdAsync(id);

        if (user?.CreatorPageData == null)
            return null;

        return new AuthorRequestDto
        {
            Id = user.Id,
            PageName = user.CreatorPageData.PageName,
            AvatarUrl = user.CreatorPageData.AvatarURL,
            Description = user.CreatorPageData.Description,
            SubscribersCount = user.CreatorPageData.SubscribersCount,
            Subscriptions = [.. user.CreatorPageData.Subscriptions.Select(s => new SubscriptionDto
            {
                Id = s.Id,
                Price = s.Price,
                PictureURL = s.PictureURL,
                Description = s.Description,
                Name = s.Name,
            })]
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
                    AvatarUrl = user.CreatorPageData.AvatarURL,
                    Description = user.CreatorPageData.Description,
                    SubscribersCount = user.CreatorPageData.SubscribersCount,
                    Subscriptions = [.. user.CreatorPageData.Subscriptions.Select(s => new SubscriptionDto
                    {
                        Id = s.Id,
                        Price = s.Price,
                        PictureURL = s.PictureURL,
                        Description = s.Description,
                        Name = s.Name,
                    })]
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
                UserName = user.Name
            });

        }

        return dtos;
    }
}
