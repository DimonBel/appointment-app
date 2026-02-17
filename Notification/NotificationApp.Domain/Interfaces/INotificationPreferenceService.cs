using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service for managing user notification preferences (Module 2.1)
/// </summary>
public interface INotificationPreferenceService
{
    Task<IEnumerable<NotificationPreference>> GetByUserIdAsync(Guid userId);
    Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, NotificationType type);
    Task<NotificationPreference> CreateOrUpdateAsync(NotificationPreference preference);
    Task SetDefaultPreferencesAsync(Guid userId);
    Task<bool> IsChannelEnabledAsync(Guid userId, NotificationType type, NotificationChannel channel);
}
