using ChatApp.Domain.Entity;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using ChatApp.Repository.Interfaces;

namespace ChatApp.Service.Services;

public class FriendshipService : IFriendshipService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository;

    public FriendshipService(IFriendshipRepository friendshipRepository, IUserRepository userRepository)
    {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
    }

    public async Task<Friendship> SendFriendRequestAsync(Guid requesterId, Guid addresseeId)
    {
        if (requesterId == addresseeId)
            throw new ArgumentException("Cannot send a friend request to yourself.");

        await EnsureLocalUserExistsAsync(requesterId, "requester");
        await EnsureLocalUserExistsAsync(addresseeId, "user");

        // Check if a friendship already exists between these users
        var existing = await _friendshipRepository.GetBetweenUsersAsync(requesterId, addresseeId);
        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
                throw new InvalidOperationException("You are already friends.");
            if (existing.Status == FriendshipStatus.Pending)
                throw new InvalidOperationException("A friend request is already pending.");
            if (existing.Status == FriendshipStatus.Blocked)
                throw new InvalidOperationException("This user is blocked.");
            // If declined, allow re-sending
            if (existing.Status == FriendshipStatus.Declined)
            {
                existing.Status = FriendshipStatus.Pending;
                existing.RequesterId = requesterId;
                existing.AddresseeId = addresseeId;
                existing.UpdatedAt = DateTime.UtcNow;
                await _friendshipRepository.UpdateAsync(existing);
                return existing;
            }
        }

        var friendship = new Friendship
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendshipStatus.Pending
        };

        return await _friendshipRepository.CreateAsync(friendship);
    }

    private async Task EnsureLocalUserExistsAsync(Guid userId, string fallbackPrefix)
    {
        var existingUser = await _userRepository.GetByIdAsync(userId);
        if (existingUser != null)
        {
            return;
        }

        var shadowUser = new User
        {
            Id = userId,
            UserName = $"{fallbackPrefix}_{userId:N}",
            Email = $"{fallbackPrefix}_{userId:N}@shadow.local",
            CreatedAt = DateTime.UtcNow,
            IsOnline = false
        };

        try
        {
            await _userRepository.CreateAsync(shadowUser);
        }
        catch
        {
            var createdUser = await _userRepository.GetByIdAsync(userId);
            if (createdUser == null)
            {
                throw new ArgumentException("User not found.");
            }
        }
    }

    public async Task<Friendship> AcceptFriendRequestAsync(Guid friendshipId, Guid userId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId)
            ?? throw new ArgumentException("Friend request not found.");

        if (friendship.AddresseeId != userId)
            throw new InvalidOperationException("Only the recipient can accept a friend request.");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("This request is no longer pending.");

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;
        await _friendshipRepository.UpdateAsync(friendship);
        return friendship;
    }

    public async Task<Friendship> DeclineFriendRequestAsync(Guid friendshipId, Guid userId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId)
            ?? throw new ArgumentException("Friend request not found.");

        if (friendship.AddresseeId != userId)
            throw new InvalidOperationException("Only the recipient can decline a friend request.");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("This request is no longer pending.");

        friendship.Status = FriendshipStatus.Declined;
        friendship.UpdatedAt = DateTime.UtcNow;
        await _friendshipRepository.UpdateAsync(friendship);
        return friendship;
    }

    public async Task RemoveFriendAsync(Guid friendshipId, Guid userId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId)
            ?? throw new ArgumentException("Friendship not found.");

        if (friendship.RequesterId != userId && friendship.AddresseeId != userId)
            throw new InvalidOperationException("You are not part of this friendship.");

        await _friendshipRepository.DeleteAsync(friendshipId);
    }

    public async Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId)
    {
        return await _friendshipRepository.GetFriendsAsync(userId);
    }

    public async Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid userId)
    {
        return await _friendshipRepository.GetPendingRequestsAsync(userId);
    }

    public async Task<IEnumerable<Friendship>> GetSentRequestsAsync(Guid userId)
    {
        return await _friendshipRepository.GetSentRequestsAsync(userId);
    }

    public async Task<bool> AreFriendsAsync(Guid userId1, Guid userId2)
    {
        return await _friendshipRepository.AreFriendsAsync(userId1, userId2);
    }

    public async Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId)
    {
        return await _friendshipRepository.GetFriendIdsAsync(userId);
    }

    public async Task<Friendship?> GetFriendshipBetweenAsync(Guid userId1, Guid userId2)
    {
        return await _friendshipRepository.GetBetweenUsersAsync(userId1, userId2);
    }
}
