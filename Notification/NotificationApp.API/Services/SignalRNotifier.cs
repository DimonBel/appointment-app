using Microsoft.AspNetCore.SignalR;
using NotificationApp.API.Hubs;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Interfaces;

namespace NotificationApp.API.Services;

/// <summary>
/// Sends real-time notifications to connected clients via SignalR NotificationHub.
/// </summary>
public class SignalRNotifier : IRealTimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotifier(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(Guid userId, Notification notification)
    {
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
        {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            type = notification.Type.ToString(),
            priority = notification.Priority.ToString(),
            status = notification.Status.ToString(),
            referenceId = notification.ReferenceId,
            referenceType = notification.ReferenceType,
            metadata = notification.Metadata,
            createdAt = notification.CreatedAt,
            sentAt = notification.SentAt
        });
    }
}
