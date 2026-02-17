using ChatApp.Domain.Entity;
using ChatApp.Domain.Enums;

namespace ChatApp.Repository.Interfaces;

public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(Guid id);
    Task<Friendship?> GetBetweenUsersAsync(Guid userId1, Guid userId2);
    Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId);
    Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid userId);
    Task<IEnumerable<Friendship>> GetSentRequestsAsync(Guid userId);
    Task<Friendship> CreateAsync(Friendship friendship);
    Task UpdateAsync(Friendship friendship);
    Task DeleteAsync(Guid id);
    Task<bool> AreFriendsAsync(Guid userId1, Guid userId2);
    Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId);
}
