using AutomationApp.Domain.Enums;

namespace AutomationApp.Domain.Entity;

/// <summary>
/// Represents a draft booking being built through conversational interaction
/// Collects booking information incrementally before final submission
/// </summary>
public class BookingDraft
{
    /// <summary>
    /// Unique identifier for the booking draft
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the conversation building this draft
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// ID of the user creating the booking
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional ID of the selected professional
    /// </summary>
    public Guid? ProfessionalId { get; set; }

    /// <summary>
    /// Optional type of service (domain configuration)
    /// </summary>
    public string? ServiceType { get; set; }

    /// <summary>
    /// Optional preferred appointment date and time
    /// </summary>
    public DateTime? PreferredDateTime { get; set; }

    /// <summary>
    /// Optional duration in minutes
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Optional notes from the client
    /// </summary>
    public string? ClientNotes { get; set; }

    /// <summary>
    /// Optional additional key-value data fields
    /// </summary>
    public Dictionary<string, string>? AdditionalData { get; set; }

    /// <summary>
    /// Status of the booking draft
    /// </summary>
    public BookingDraftStatus Status { get; set; } = BookingDraftStatus.InProgress;

    /// <summary>
    /// Timestamp when draft was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when draft was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// ID of the final order after submission
    /// Null until successfully submitted
    /// </summary>
    public Guid? FinalOrderId { get; set; }

    // Navigation properties

    /// <summary>
    /// The conversation building this draft
    /// </summary>
    public Conversation? Conversation { get; set; }
}

/// <summary>
/// Status of a booking draft in the automation flow
/// </summary>
public enum BookingDraftStatus
{
    /// <summary>
    /// Draft is being actively built through conversation
    /// </summary>
    InProgress = 0,

    /// <summary>
    /// All required information collected, ready to submit
    /// </summary>
    ReadyForSubmission = 1,

    /// <summary>
    /// Draft has been submitted to create an order
    /// </summary>
    Submitted = 2,

    /// <summary>
    /// Booking process completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Booking draft was cancelled
    /// </summary>
    Cancelled = 4
}