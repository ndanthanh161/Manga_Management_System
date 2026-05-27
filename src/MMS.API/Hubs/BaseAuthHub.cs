using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MMS.API.Hubs;

[Authorize]
public abstract class BaseAuthHub : Hub
{
    protected Guid CurrentUserId =>
        Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public override async Task OnConnectedAsync()
    {
        // Auto join personal room để server push riêng cho user này
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{CurrentUserId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{CurrentUserId}");
        await base.OnDisconnectedAsync(exception);
    }
}
