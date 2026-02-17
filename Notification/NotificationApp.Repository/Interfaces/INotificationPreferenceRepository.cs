using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Repository.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<IEnumerable<NotificationPreference>> GetByUserIdAsync(Guid userId);
    Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, NotificationType type);
    Task<NotificationPreference> CreateAsync(NotificationPreference preference);
    Task<NotificationPreference> UpdateAsync(NotificationPreference preference);
    Task DeleteByUserIdAsync(Guid userId);
}
