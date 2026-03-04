using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing user notification preferences
/// Controls how users receive notifications (email, in-app, etc.) for different types
/// </summary>
public interface INotificationPreferenceService
{
    /// <summary>
    /// Retrieves all notification preferences for a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Collection of user's notification preferences</returns>
    Task<IEnumerable<NotificationPreference>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Retrieves a specific notification preference for a user and notification type
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="type">Notification type</param>
    /// <returns>Notification preference if found, null otherwise</returns>
    Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, NotificationType type);

    /// <summary>
    /// Creates or updates a notification preference
    /// If preference exists, updates it; otherwise creates new
    /// </summary>
    /// <param name="preference">Notification preference to save</param>
    /// <returns>Saved notification preference</returns>
    Task<NotificationPreference> CreateOrUpdateAsync(NotificationPreference preference);

    /// <summary>
    /// Sets default notification preferences for a new user
    /// Enables common channels for common notification types
    /// </summary>
    /// <param name="userId">ID of the user</param>
    Task SetDefaultPreferencesAsync(Guid userId);

    /// <summary>
    /// Checks if a specific channel is enabled for a user and notification type
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="type">Notification type</param>
    /// <param name="channel">Notification channel to check</param>
    /// <returns>True if channel is enabled, false otherwise</returns>
    Task<bool> IsChannelEnabledAsync(Guid userId, NotificationType type, NotificationChannel channel);
}