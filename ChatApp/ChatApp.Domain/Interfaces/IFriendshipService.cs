using ChatApp.Domain.Entity;
using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Interfaces;

public interface IFriendshipService
{
    Task<Friendship> SendFriendRequestAsync(Guid requesterId, Guid addresseeId);
    Task<Friendship> AcceptFriendRequestAsync(Guid friendshipId, Guid userId);
    Task<Friendship> DeclineFriendRequestAsync(Guid friendshipId, Guid userId);
    Task RemoveFriendAsync(Guid friendshipId, Guid userId);
    Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId);
    Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid userId);
    Task<IEnumerable<Friendship>> GetSentRequestsAsync(Guid userId);
    Task<bool> AreFriendsAsync(Guid userId1, Guid userId2);
    Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId);
    Task<Friendship?> GetFriendshipBetweenAsync(Guid userId1, Guid userId2);
}
