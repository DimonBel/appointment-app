using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// Notification template with dynamic placeholders
/// Module 2.4 - Template Management
/// Enables reusable notification content with variable substitution
/// </summary>
public class NotificationTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique template key (e.g., "order_approved", "appointment_reminder")
    /// Used to reference templates programmatically
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the template
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template for the notification title with placeholders like {PatientName}, {DoctorName}
    /// </summary>
    public string TitleTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Template for the notification body with placeholders
    /// </summary>
    public string BodyTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification this template is for
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Whether this template is currently active
    /// Inactive templates cannot be used
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when template was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when template was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Notifications created using this template
    /// </summary>
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}