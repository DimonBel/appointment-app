namespace AutomationApp.API.DTOs;

/// <summary>
/// DTO for sending a message to the AI assistant
/// </summary>
public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public string? SelectedOption { get; set; }
}