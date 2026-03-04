namespace AutomationApp.API.DTOs;

/// <summary>
/// DTO representing a message in an AI conversation
/// </summary>
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