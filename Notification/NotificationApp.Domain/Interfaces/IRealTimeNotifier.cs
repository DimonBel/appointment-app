using NotificationApp.Domain.Entity;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Abstraction for sending real-time notifications to connected clients (e.g., via SignalR).
/// </summary>
public interface IRealTimeNotifier
{
    Task SendToUserAsync(Guid userId, Notification notification);
}
