using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// Notification template with dynamic placeholders.
/// Module 2.4 - Template Management
/// </summary>
public class NotificationTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique template key (e.g., "order_approved", "appointment_reminder")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template for the notification title with placeholders like {PatientName}, {DoctorName}
    /// </summary>
    public string TitleTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Template for the notification body with placeholders
    /// </summary>
    public string BodyTemplate { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    /// <summary>
    /// Whether this template is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
