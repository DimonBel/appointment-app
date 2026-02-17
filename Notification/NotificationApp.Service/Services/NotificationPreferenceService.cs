using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Domain.Interfaces;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Service.Services;

public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly INotificationPreferenceRepository _preferenceRepository;

    public NotificationPreferenceService(INotificationPreferenceRepository preferenceRepository)
    {
        _preferenceRepository = preferenceRepository;
    }

    public async Task<IEnumerable<NotificationPreference>> GetByUserIdAsync(Guid userId)
    {
        return await _preferenceRepository.GetByUserIdAsync(userId);
    }

    public async Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, NotificationType type)
    {
        return await _preferenceRepository.GetByUserAndTypeAsync(userId, type);
    }

    public async Task<NotificationPreference> CreateOrUpdateAsync(NotificationPreference preference)
    {
        var existing = await _preferenceRepository.GetByUserAndTypeAsync(
            preference.UserId, preference.NotificationType);

        if (existing != null)
        {
            existing.InAppEnabled = preference.InAppEnabled;
            existing.EmailEnabled = preference.EmailEnabled;
            existing.PushEnabled = preference.PushEnabled;
            return await _preferenceRepository.UpdateAsync(existing);
        }

        return await _preferenceRepository.CreateAsync(preference);
    }

    public async Task SetDefaultPreferencesAsync(Guid userId)
    {
        // Create default preferences for all notification types
        foreach (NotificationType type in Enum.GetValues<NotificationType>())
        {
            var existing = await _preferenceRepository.GetByUserAndTypeAsync(userId, type);
            if (existing == null)
            {
                await _preferenceRepository.CreateAsync(new NotificationPreference
                {
                    UserId = userId,
                    NotificationType = type,
                    InAppEnabled = true,
                    EmailEnabled = type == NotificationType.OrderApproved ||
                                   type == NotificationType.OrderDeclined ||
                                   type == NotificationType.OrderReminder,
                    PushEnabled = false
                });
            }
        }
    }

    public async Task<bool> IsChannelEnabledAsync(Guid userId, NotificationType type, NotificationChannel channel)
    {
        var preference = await _preferenceRepository.GetByUserAndTypeAsync(userId, type);

        // If no preferences set, default to enabled for in-app
        if (preference == null)
        {
            return channel == NotificationChannel.InApp;
        }

        return channel switch
        {
            NotificationChannel.InApp => preference.InAppEnabled,
            NotificationChannel.Email => preference.EmailEnabled,
            NotificationChannel.Push => preference.PushEnabled,
            _ => false
        };
    }
}
