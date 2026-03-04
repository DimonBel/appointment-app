using ChatApp.Domain.Entity;
using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing friendship relationships between users
/// Handles friend requests, acceptance, declination, and friendship management
/// </summary>
public interface IFriendshipService
{
    /// <summary>
    /// Sends a friend request from one user to another
    /// Creates a pending friendship that requires acceptance
    /// </summary>
    /// <param name="requesterId">ID of the user sending the request</param>
    /// <param name="addresseeId">ID of the user receiving the request</param>
    /// <returns>Created friendship with Pending status</returns>
    Task<Friendship> SendFriendRequestAsync(Guid requesterId, Guid addresseeId);

    /// <summary>
    /// Accepts a pending friend request
    /// Transitions friendship from Pending to Accepted
    /// </summary>
    /// <param name="friendshipId">ID of the friendship to accept</param>
    /// <param name="userId">ID of the user accepting the request</param>
    /// <returns>Accepted friendship</returns>
    Task<Friendship> AcceptFriendRequestAsync(Guid friendshipId, Guid userId);

    /// <summary>
    /// Declines a pending friend request
    /// Transitions friendship from Pending to Declined
    /// </summary>
    /// <param name="friendshipId">ID of the friendship to decline</param>
    /// <param name="userId">ID of the user declining the request</param>
    /// <returns>Declined friendship</returns>
    Task<Friendship> DeclineFriendRequestAsync(Guid friendshipId, Guid userId);

    /// <summary>
    /// Removes a friendship connection
    /// Deactivates the friendship relationship
    /// </summary>
    /// <param name="friendshipId">ID of the friendship to remove</param>
    /// <param name="userId">ID of the user initiating the removal</param>
    Task RemoveFriendAsync(Guid friendshipId, Guid userId);

    /// <summary>
    /// Retrieves all friends for a user
    /// Returns only accepted friendships
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Collection of user's friends</returns>
    Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId);

    /// <summary>
    /// Retrieves all pending friend requests for a user
    /// Requests where the user is the addressee
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Collection of pending friend requests</returns>
    Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid userId);

    /// <summary>
    /// Retrieves all sent friend requests for a user
    /// Requests where the user is the requester
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Collection of sent friend requests</returns>
    Task<IEnumerable<Friendship>> GetSentRequestsAsync(Guid userId);

    /// <summary>
    /// Checks if two users are friends
    /// Returns true only for accepted friendships
    /// </summary>
    /// <param name="userId1">ID of the first user</param>
    /// <param name="userId2">ID of the second user</param>
    /// <returns>True if users are friends, false otherwise</returns>
    Task<bool> AreFriendsAsync(Guid userId1, Guid userId2);

    /// <summary>
    /// Retrieves all friend IDs for a user
    /// Returns only IDs for efficiency in batch operations
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Collection of friend user IDs</returns>
    Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId);

    /// <summary>
    /// Retrieves the friendship between two users
    /// Returns friendship in any state (Pending, Accepted, Declined)
    /// </summary>
    /// <param name="userId1">ID of the first user</param>
    /// <param name="userId2">ID of the second user</param>
    /// <returns>Friendship if found, null otherwise</returns>
    Task<Friendship?> GetFriendshipBetweenAsync(Guid userId1, Guid userId2);
}