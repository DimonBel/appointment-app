using NotificationApp.Domain.Entity;

namespace NotificationApp.Repository.Interfaces;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate> CreateAsync(NotificationTemplate template);
    Task<NotificationTemplate?> GetByIdAsync(Guid id);
    Task<NotificationTemplate?> GetByKeyAsync(string key);
    Task<IEnumerable<NotificationTemplate>> GetAllAsync();
    Task<IEnumerable<NotificationTemplate>> GetByTypeAsync(Domain.Enums.NotificationType type);
    Task<NotificationTemplate> UpdateAsync(NotificationTemplate template);
    Task DeleteAsync(Guid id);
}
