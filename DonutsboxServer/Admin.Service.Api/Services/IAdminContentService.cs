using Admin.Service.Api.Dto;

namespace Admin.Service.Api.Services;

/// <summary>
/// Интерфейс сервиса для администрирования контента
/// </summary>
public interface IAdminContentService
{
    /// <summary>
    /// Получить список всех постов
    /// </summary>
    Task<IEnumerable<AdminContentPostListDto>> GetAllPostsAsync();

    /// <summary>
    /// Получить информацию о конкретном посте
    /// </summary>
    Task<AdminContentPostListDto?> GetPostByIdAsync(Guid postId);

    /// <summary>
    /// Удалить пост
    /// </summary>
    Task<AdminDeleteResultDto> DeletePostAsync(Guid postId);

    /// <summary>
    /// Массовое удаление постов
    /// </summary>
    Task<AdminDeleteResultDto> DeletePostsAsync(List<Guid> postIds);

    /// <summary>
    /// Удалить все посты автора
    /// </summary>
    Task<AdminDeleteResultDto> DeleteCreatorPostsAsync(Guid creatorPageDataId);

    /// <summary>
    /// Добавить пост в теневой бан
    /// </summary>
    Task<AdminActionResponseDto> ShadowBanPostAsync(Guid postId);

    /// <summary>
    /// Снять теневой бан с поста
    /// </summary>
    Task<AdminActionResponseDto> UnshadowBanPostAsync(Guid postId);
}
