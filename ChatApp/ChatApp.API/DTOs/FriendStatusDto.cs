namespace ChatApp.API.DTOs;

/// <summary>
/// DTO representing the friendship status between current user and another user
/// </summary>
public record FriendStatusDto(
    Guid UserId,
    string Status, // "none", "pending_sent", "pending_received", "friends"
    Guid? FriendshipId
);