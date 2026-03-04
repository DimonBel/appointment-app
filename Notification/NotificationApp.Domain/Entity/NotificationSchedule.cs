using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// Scheduled notification (reminders, delayed notifications)
/// Module 2.3 - Notification Schedule
/// Handles delayed and time-based notifications
/// </summary>
public class NotificationSchedule
{
    /// <summary>
    /// Unique identifier for the schedule
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the user to notify
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Related entity ID (e.g., OrderId for appointment reminders)
    /// </summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// Type of the referenced entity
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Type of notification to send
    /// </summary>
    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// When this notification should be sent
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Whether this schedule has been processed
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// Whether this schedule was cancelled before processing
    /// </summary>
    public bool IsCancelled { get; set; } = false;

    /// <summary>
    /// Optional template to use for the notification
    /// </summary>
    public Guid? TemplateId { get; set; }

    /// <summary>
    /// Additional data for template rendering (JSON)
    /// </summary>
    public string? TemplateData { get; set; }

    /// <summary>
    /// Timestamp when schedule was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when schedule was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}