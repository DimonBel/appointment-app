using NotificationApp.Domain.Entity;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Abstraction for sending real-time notifications to connected clients
/// Used for immediate delivery via SignalR or similar technologies
/// </summary>
public interface IRealTimeNotifier
{
    /// <summary>
    /// Sends a notification to a specific user in real-time
    /// Delivers immediately to connected clients
    /// </summary>
    /// <param name="userId">ID of the user to notify</param>
    /// <param name="notification">Notification to send</param>
    Task SendToUserAsync(Guid userId, Notification notification);
}