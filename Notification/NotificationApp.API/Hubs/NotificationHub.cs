using Microsoft.AspNetCore.SignalR;

namespace NotificationApp.API.Hubs;

/// <summary>
/// SignalR Hub for real-time notification delivery to connected clients.
/// </summary>
public class NotificationHub : Hub
{
    private static readonly Dictionary<Guid, HashSet<string>> _userConnections = new();
    private static readonly object _lock = new();

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            lock (_lock)
            {
                if (!_userConnections.ContainsKey(userId))
                    _userConnections[userId] = new HashSet<string>();
                _userConnections[userId].Add(Context.ConnectionId);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            lock (_lock)
            {
                if (_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].Remove(Context.ConnectionId);
                    if (_userConnections[userId].Count == 0)
                        _userConnections.Remove(userId);
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Mark a notification as read from the client side
    /// </summary>
    public async Task MarkAsRead(string notificationId)
    {
        // This will be handled by the API endpoint
        await Clients.Caller.SendAsync("NotificationMarkedRead", notificationId);
    }

    /// <summary>
    /// Static helper to check if a user is connected
    /// </summary>
    public static bool IsUserConnected(Guid userId)
    {
        lock (_lock)
        {
            return _userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0;
        }
    }
}
