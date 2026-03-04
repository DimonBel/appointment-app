using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entity;

/// <summary>
/// Represents a friendship/connection between two users
/// Users can only chat after a friend request is accepted
/// </summary>
public class Friendship
{
    /// <summary>
    /// Unique identifier for the friendship
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The user who sent the friend request
    /// </summary>
    public Guid RequesterId { get; set; }

    /// <summary>
    /// The user who receives the friend request
    /// </summary>
    public Guid AddresseeId { get; set; }

    /// <summary>
    /// Current status of the friendship
    /// Pending, Accepted, or Declined
    /// </summary>
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    /// <summary>
    /// Timestamp when friendship was created (request sent)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when friendship status was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}