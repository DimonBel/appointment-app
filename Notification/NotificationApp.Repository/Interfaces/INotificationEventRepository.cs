using NotificationApp.Domain.Entity;

namespace NotificationApp.Repository.Interfaces;

public interface INotificationEventRepository
{
    Task<NotificationEvent> CreateAsync(NotificationEvent notificationEvent);
    Task<NotificationEvent?> GetByIdAsync(Guid id);
    Task<IEnumerable<NotificationEvent>> GetUnprocessedAsync();
    Task<IEnumerable<NotificationEvent>> GetFailedAsync(int maxRetries = 3);
    Task<NotificationEvent> UpdateAsync(NotificationEvent notificationEvent);
}
