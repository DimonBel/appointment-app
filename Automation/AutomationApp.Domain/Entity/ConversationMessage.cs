namespace AutomationApp.Domain.Entity;

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsFromUser { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public List<string>? SuggestedOptions { get; set; }
    public string? SelectedOption { get; set; }
    public string? Intent { get; set; }

    // Navigation properties
    public Conversation? Conversation { get; set; }
}