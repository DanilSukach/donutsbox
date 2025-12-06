using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Donutsbox.Api.Hubs;

[Authorize]
public class MediaProcessingHub : Hub
{
    /// <summary>
    /// Присоединяет пользователя к группе для получения уведомлений о статусе обработки медиа
    /// </summary>
    public async Task JoinUserGroup()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    /// <summary>
    /// Отключает пользователя от группы
    /// </summary>
    public async Task LeaveUserGroup()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
}

