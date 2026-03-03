using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// User notification preferences per channel and type
/// Module 2.1 - Notification Preferences
/// Controls how users receive notifications for different types
/// </summary>
public class NotificationPreference
{
    /// <summary>
    /// Unique identifier for the preference
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the user this preference belongs to
    /// </summary>
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

    /// <summary>
    /// Timestamp when preference was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when preference was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}