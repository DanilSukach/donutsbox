using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Donutsbox.Api.Hubs;

[Authorize]
public class CommentsHub : Hub
{
    public async Task JoinPostComments(string postId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"post-{postId}");
    }

    public async Task LeavePostComments(string postId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post-{postId}");
    }
}
