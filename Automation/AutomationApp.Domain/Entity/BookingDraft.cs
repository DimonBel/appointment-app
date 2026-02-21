using AutomationApp.Domain.Enums;

namespace AutomationApp.Domain.Entity;

public class BookingDraft
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ProfessionalId { get; set; }
    public string? ServiceType { get; set; }
    public DateTime? PreferredDateTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? ClientNotes { get; set; }
    public Dictionary<string, string>? AdditionalData { get; set; }
    public BookingDraftStatus Status { get; set; } = BookingDraftStatus.InProgress;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? FinalOrderId { get; set; }

    // Navigation properties
    public Conversation? Conversation { get; set; }
}

public enum BookingDraftStatus
{
    InProgress = 0,
    ReadyForSubmission = 1,
    Submitted = 2,
    Completed = 3,
    Cancelled = 4
}