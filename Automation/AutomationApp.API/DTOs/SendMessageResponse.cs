namespace AutomationApp.API.DTOs;

/// <summary>
/// DTO response from sending a message to the AI assistant
/// </summary>
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