using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories;

namespace Donutsbox.Api.Services;

public class ContentPostService(IEntityRepository<ContentPost, Guid> repository, IMapper mapper) : IEntityService<ContentPostDto, Guid>
{
    public async Task<ContentPostDto?> AddAsync(ContentPostDto entity)
    {
        var post = mapper.Map<ContentPost>(entity);
        var addedPost = await repository.AddAsync(post);
        return mapper.Map<ContentPostDto>(addedPost);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var post = await repository.GetByIdAsync(id);
        if (post == null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<IEnumerable<ContentPostDto>> GetAllAsync()
    {
        var posts = await repository.GetAllAsync();
        return posts.Select(mapper.Map<ContentPostDto>);
    }

    public async Task<ContentPostDto?> GetByIdAsync(Guid id)
    {
        var post = await repository.GetByIdAsync(id);
        return mapper.Map<ContentPostDto>(post);
    }

    public async Task<bool> UpdateAsync(ContentPostDto entity, Guid id)
    {
        var existingPost = await repository.GetByIdAsync(id);
        if (existingPost == null) return false;

        var updatedPost = mapper.Map<ContentPost>(entity);
        updatedPost.PostId = id;

        return await repository.UpdateAsync(updatedPost, id);
    }
}
