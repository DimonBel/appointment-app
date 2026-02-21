namespace AutomationApp.API.DTOs;

public class ConversationDTO
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string State { get; set; } = string.Empty;
    public string? DetectedIntent { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; }
}

public class ConversationMessageDTO
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsFromUser { get; set; }
    public DateTime SentAt { get; set; }
    public List<string>? SuggestedOptions { get; set; }
    public string? SelectedOption { get; set; }
}

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public string? SelectedOption { get; set; }
}

public class SendMessageResponse
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public string ResponseText { get; set; } = string.Empty;
    public List<string> SuggestedOptions { get; set; } = new();
    public string CurrentState { get; set; } = string.Empty;
    public bool IsBookingComplete { get; set; }
    public Guid? OrderId { get; set; }
}

public class BookingDraftDTO
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? ProfessionalId { get; set; }
    public string? ServiceType { get; set; }
    public DateTime? PreferredDateTime { get; set; }
    public string? ClientNotes { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? FinalOrderId { get; set; }
}

public class SubmitBookingRequest
{
    public Guid ConversationId { get; set; }
}