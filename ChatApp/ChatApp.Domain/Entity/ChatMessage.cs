namespace ChatApp.Domain.Entity;

/// <summary>
/// Represents a chat message between two users
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Message content/text
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when message was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the user who sent the message
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// ID of the user who received the message
    /// </summary>
    public Guid ReceiverId { get; set; }

    /// <summary>
    /// Whether the message has been read by the receiver
    /// </summary>
    public bool IsRead { get; set; } = false;

    // Navigation properties - will be populated by repository

    /// <summary>
    /// The user who sent the message
    /// </summary>
    public User? Sender { get; set; }

    /// <summary>
    /// The user who received the message
    /// </summary>
    public User? Receiver { get; set; }
}