using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// Represents a notification sent to a user
/// Module 2.2 - Notification Delivery
/// Supports multiple delivery channels (InApp, Email, RealTime)
/// </summary>
public class Notification
{
    /// <summary>
    /// Unique identifier for the notification
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the user to notify
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message body
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification (OrderUpdate, Reminder, etc.)
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Delivery channel (InApp, Email, RealTime, or All)
    /// </summary>
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    /// <summary>
    /// Current status of the notification
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// Priority level of the notification
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Optional reference to related entity (e.g., OrderId, ChatId)
    /// </summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// Type of the referenced entity (e.g., "Order", "Chat", "Profile")
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Additional metadata stored as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Timestamp when notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when notification was sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Timestamp when notification was read by user
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Timestamp when notification is scheduled to be sent
    /// For delayed notifications
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// ID of the notification template used (if any)
    /// </summary>
    public Guid? TemplateId { get; set; }

    /// <summary>
    /// The notification template used (if any)
    /// </summary>
    public NotificationTemplate? Template { get; set; }
}