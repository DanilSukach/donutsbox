using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthorRepository;
using Donutsbox.Domain.Repositories.EntityRepository;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Donutsbox.Api.Services.AuthorService;

public class AuthorService(IAuthorRepository authorRepository, IEntityRepository<User, Guid> userRepository, IEntityRepository<CreatorPageData, Guid> creatorRepository, IEntityRepository<Subscription, Guid> subcriptionRepository) :IAuthorService
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

    public async Task<SubscriptionDto> AddSubscriptionAsync(SubscriptionDto dto, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found");
        var userId = Guid.Parse(userIdClaim.Value);
        var author = await authorRepository.GetByIdAsync(userId);

        var subcription = new Subscription
        {
            Id = Guid.NewGuid(),
            CreatorPageData = author!.CreatorPageData!,
            CreatorPageDataId = author!.CreatorPageData!.Id,
            Price = dto.Price,
            Name = dto.Name,
            Description = dto.Description,
            PictureURL = dto.PictureURL!
        };
        var result = await subcriptionRepository.AddAsync(subcription);
        return new SubscriptionDto
        {
            Description = result.Description,
            PictureURL = result.PictureURL,
            Price = result.Price,
            Name = result.Name,
        };
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
                    SubscribersCount = user.CreatorPageData.SubscribersCount
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
            SubscribersCount = user.CreatorPageData.SubscribersCount
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
                    SubscribersCount = user.CreatorPageData.SubscribersCount
                });
            }
        }

        return dtos;
    }
}
