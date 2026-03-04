using AutomationApp.Domain.Enums;

namespace AutomationApp.Domain.Entity;

/// <summary>
/// Represents a conversational session with the AI booking assistant
/// Tracks the state and context of the booking conversation
/// </summary>
public class Conversation
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the user having this conversation
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current state of the conversation
    /// Determines what the AI expects next from the user
    /// </summary>
    public ConversationState State { get; set; } = ConversationState.Greeting;

    /// <summary>
    /// Last detected user intent
    /// Helps AI understand what the user wants to do
    /// </summary>
    public UserIntent? DetectedIntent { get; set; }

    /// <summary>
    /// Timestamp when conversation started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of last activity in the conversation
    /// </summary>
    public DateTime? LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this conversation is still active
    /// False indicates completed or abandoned conversation
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Context data accumulated during conversation
    /// Stores extracted booking information, user preferences, etc.
    /// </summary>
    public Dictionary<string, object>? ContextData { get; set; }

    // Navigation properties

    /// <summary>
    /// All messages in this conversation
    /// </summary>
    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}