namespace Donutsbox.Api.Dto;

public class UpdatePostAccessRequestDto
{
    /// <summary>
    /// Сделать пост публичным для всех пользователей
    /// </summary>
    public bool IsPublic { get; set; } = true;
    /// <summary>
    /// Разрешенные уровни подписок (используются, если IsPublic == false).
    /// Пустой список означает доступ для всех активных подписчиков создателя.
    /// </summary>
    public List<Guid> AllowedSubscriptionIds { get; set; } = [];
}

