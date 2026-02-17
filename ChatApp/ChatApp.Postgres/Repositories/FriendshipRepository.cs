using ChatApp.Domain.Entity;
using ChatApp.Domain.Enums;
using ChatApp.Postgres.Data;
using ChatApp.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Postgres.Repositories;

public class FriendshipRepository : IFriendshipRepository
{
    private readonly AppDbContext _context;

    public FriendshipRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Friendship?> GetByIdAsync(Guid id)
    {
        return await _context.Friendships.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Friendship?> GetBetweenUsersAsync(Guid userId1, Guid userId2)
    {
        return await _context.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == userId1 && f.AddresseeId == userId2) ||
            (f.RequesterId == userId2 && f.AddresseeId == userId1));
    }

    public async Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId)
    {
        return await _context.Friendships
            .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid userId)
    {
        return await _context.Friendships
            .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Friendship>> GetSentRequestsAsync(Guid userId)
    {
        return await _context.Friendships
            .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<Friendship> CreateAsync(Friendship friendship)
    {
        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync();
        return friendship;
    }

    public async Task UpdateAsync(Friendship friendship)
    {
        friendship.UpdatedAt = DateTime.UtcNow;
        _context.Friendships.Update(friendship);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var friendship = await _context.Friendships.FindAsync(id);
        if (friendship != null)
        {
            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> AreFriendsAsync(Guid userId1, Guid userId2)
    {
        return await _context.Friendships.AnyAsync(f =>
            ((f.RequesterId == userId1 && f.AddresseeId == userId2) ||
             (f.RequesterId == userId2 && f.AddresseeId == userId1)) &&
            f.Status == FriendshipStatus.Accepted);
    }

    public async Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId)
    {
        var friendships = await _context.Friendships
            .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
            .ToListAsync();

        return friendships.Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId);
    }
}
