using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp.Domain.Entity;

/// <summary>
/// Represents a user in the Chat application
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display username
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// URL of the user's avatar image
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Timestamp when user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the user is currently online
    /// </summary>
    public bool IsOnline { get; set; } = false;

    /// <summary>
    /// Messages sent by this user
    /// Not persisted to database (calculated property)
    /// </summary>
    [NotMapped]
    public ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// Messages received by this user
    /// Not persisted to database (calculated property)
    /// </summary>
    [NotMapped]
    public ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
}