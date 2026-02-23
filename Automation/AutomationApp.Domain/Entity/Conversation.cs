using AutomationApp.Domain.Enums;

namespace AutomationApp.Domain.Entity;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ConversationState State { get; set; } = ConversationState.Greeting;
    public UserIntent? DetectedIntent { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object>? ContextData { get; set; }

    // Navigation properties
    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}