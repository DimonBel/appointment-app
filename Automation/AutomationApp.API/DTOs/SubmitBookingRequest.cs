namespace AutomationApp.API.DTOs;

/// <summary>
/// DTO for submitting a booking draft to create an actual order
/// </summary>
public class SubmitBookingRequest
{
    public Guid ConversationId { get; set; }
}