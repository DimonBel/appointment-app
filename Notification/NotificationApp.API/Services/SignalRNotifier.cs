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
        var typeString = notification.Type.ToString();
        System.Console.WriteLine($"[SignalRNotifier] Sending notification to user {userId}:");
        System.Console.WriteLine($"  Type={typeString}, Title={notification.Title}");
        System.Console.WriteLine($"  Message={notification.Message}");
        System.Console.WriteLine($"  ReferenceId={notification.ReferenceId}, ReferenceType={notification.ReferenceType}");
        System.Console.WriteLine($"  Metadata={notification.Metadata}");

        try
        {
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = typeString,
                priority = notification.Priority.ToString(),
                status = notification.Status.ToString(),
                referenceId = notification.ReferenceId,
                referenceType = notification.ReferenceType,
                metadata = notification.Metadata,
                createdAt = notification.CreatedAt,
                sentAt = notification.SentAt
            });
            System.Console.WriteLine($"[SignalRNotifier] Successfully sent notification to user_{userId}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[SignalRNotifier] ERROR sending notification: {ex.Message}");
            System.Console.WriteLine($"[SignalRNotifier] Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
