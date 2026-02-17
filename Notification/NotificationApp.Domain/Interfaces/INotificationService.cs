using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service for managing notifications (Module 2.2 - Notification Delivery)
/// </summary>
public interface INotificationService
{
    Task<Notification> CreateAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid id);
    Task MarkAllAsReadAsync(Guid userId);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Notification>> GetByTypeAsync(Guid userId, NotificationType type);

    /// <summary>
    /// Send notification respecting user preferences
    /// </summary>
    Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message,
        Guid? referenceId = null, string? referenceType = null, string? metadata = null);
}
