using Admin.Service.Api.Dto;
namespace Admin.Service.Api.Services;

/// <summary>
/// Интерфейс сервиса для администрирования пользователей
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Получить список всех пользователей с детальной информацией
    /// </summary>
    Task<IEnumerable<AdminUserListDto>> GetAllUsersAsync();

    /// <summary>
    /// Получить список всех авторов с детальной информацией
    /// </summary>
    Task<IEnumerable<AdminAuthorListDto>> GetAllAuthors();

    /// <summary>
    /// Получить информацию о конкретном пользователе
    /// </summary>
    Task<AdminUserListDto?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Удалить пользователя и все связанные с ним данные
    /// </summary>
    Task<AdminDeleteResultDto> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Массовое удаление пользователей
    /// </summary>
    Task<AdminDeleteResultDto> DeleteUsersAsync(List<Guid> userIds);

    /// <summary>
    /// Добавить автора в теневой бан
    /// </summary>
    Task<AdminActionResponseDto> ShadowBanAuthorAsync(Guid creatorPageId);

    /// <summary>
    /// Снять теневой бан с автора
    /// </summary>
    Task<AdminActionResponseDto> UnshadowBanAuthorAsync(Guid creatorPageId);
}