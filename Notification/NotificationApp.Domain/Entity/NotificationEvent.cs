using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Entity;

/// <summary>
/// Records events received from other microservices
/// Module 2.5 - Event Listener
/// Part of event-driven architecture for cross-service communication
/// </summary>
public class NotificationEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Source microservice (e.g., "AppointmentService", "ChatService", "IdentityService")
    /// </summary>
    public string SourceService { get; set; } = string.Empty;

    /// <summary>
    /// Event name (e.g., "OrderCreated", "OrderApproved", "MessageReceived")
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Event payload as JSON
    /// Contains data needed to process the event
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Whether this event has been processed into notifications
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of processing attempts
    /// Used for retry logic
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Timestamp when event was received
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when event was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}