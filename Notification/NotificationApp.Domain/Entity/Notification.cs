using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// Represents a notification sent to a user.
/// Module 2.2 - Notification Delivery
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// ID of the notification template used (if any)
    /// </summary>
    public Guid? TemplateId { get; set; }
    public NotificationTemplate? Template { get; set; }
}
