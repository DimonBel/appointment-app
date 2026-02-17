using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// User notification preferences per channel and type.
/// Module 2.1 - Notification Preferences
/// </summary>
public class NotificationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    /// <summary>
    /// The notification type this preference applies to
    /// </summary>
    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// Whether in-app notifications are enabled for this type
    /// </summary>
    public bool InAppEnabled { get; set; } = true;

    /// <summary>
    /// Whether email notifications are enabled for this type
    /// </summary>
    public bool EmailEnabled { get; set; } = false;

    /// <summary>
    /// Whether push notifications are enabled for this type
    /// </summary>
    public bool PushEnabled { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
