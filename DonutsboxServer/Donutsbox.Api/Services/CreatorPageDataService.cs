using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories;

namespace Donutsbox.Api.Services;

public class CreatorPageDataService(IEntityRepository<CreatorPageData, Guid> repository, IMapper mapper) : IEntityService<CreatorPageDataDto, Guid>
{
    public async Task<CreatorPageDataDto?> AddAsync(CreatorPageDataDto entity)
    {
        var page = mapper.Map<CreatorPageData>(entity);
        var addedUser = await repository.AddAsync(page);
        return mapper.Map<CreatorPageDataDto>(addedUser);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<CreatorPageDataDto>> GetAllAsync()
    {
        var pages = await repository.GetAllAsync();
        return pages.Select(mapper.Map<CreatorPageDataDto>);
    }

    public async Task<CreatorPageDataDto?> GetByIdAsync(Guid id)
    {
        var page = await repository.GetByIdAsync(id);
        return mapper.Map<CreatorPageDataDto>(page);
    }

    public async Task<bool> UpdateAsync(CreatorPageDataDto entity, Guid id)
    {
        var updatedPage = mapper.Map<CreatorPageData>(entity);
        return await repository.UpdateAsync(updatedPage, id);
    }
}