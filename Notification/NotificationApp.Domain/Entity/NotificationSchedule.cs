using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// Scheduled notification (reminders, delayed notifications).
/// Module 2.3 - Notification Schedule
/// </summary>
public class NotificationSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    /// <summary>
    /// Related entity ID (e.g., OrderId for appointment reminders)
    /// </summary>
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }

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
    /// Optional template to use
    /// </summary>
    public Guid? TemplateId { get; set; }

    /// <summary>
    /// Additional data for template rendering (JSON)
    /// </summary>
    public string? TemplateData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
