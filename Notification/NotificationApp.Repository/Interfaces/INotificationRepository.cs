using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Repository.Interfaces;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId, int page, int pageSize);
    Task<Notification> UpdateAsync(Notification notification);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Notification>> GetByTypeAsync(Guid userId, NotificationType type);
    Task MarkAllAsReadAsync(Guid userId);
}
