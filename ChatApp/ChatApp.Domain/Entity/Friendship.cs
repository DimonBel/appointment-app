using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entity;

/// <summary>
/// Represents a friendship/connection between two users.
/// Users can only chat after a friend request is accepted.
/// </summary>
public class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The user who sent the friend request
    /// </summary>
    public Guid RequesterId { get; set; }

    /// <summary>
    /// The user who receives the friend request
    /// </summary>
    public Guid AddresseeId { get; set; }

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
