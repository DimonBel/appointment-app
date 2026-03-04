namespace AutomationApp.Domain.Entity;

/// <summary>
/// Represents a single message in a booking conversation
/// Can be from user or AI, with optional interactive options
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the conversation this message belongs to
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Message content/text
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// True if message is from user, false if from AI
    /// </summary>
    public bool IsFromUser { get; set; }

    /// <summary>
    /// Timestamp when message was sent
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional suggested response options
    /// Used for interactive conversation flow
    /// </summary>
    public List<string>? SuggestedOptions { get; set; }

    /// <summary>
    /// Optional selected option from previous suggestions
    /// Indicates user's choice from the last AI response
    /// </summary>
    public string? SelectedOption { get; set; }

    /// <summary>
    /// Optional detected intent of the message
    /// </summary>
    public string? Intent { get; set; }

    // Navigation properties

    /// <summary>
    /// The conversation this message belongs to
    /// </summary>
    public Conversation? Conversation { get; set; }
}