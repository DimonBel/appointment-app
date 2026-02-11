using ChatApp.Domain.Entity;
using ChatApp.Repository.Interfaces;
using ChatApp.Postgres.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChatApp.Postgres.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly IMemoryCache _cache;
    private const int CacheExpirationMinutes = 5;

    public ChatMessageRepository(AppDbContext context, UserManager<AppIdentityUser> userManager, IMemoryCache cache)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<ChatMessage?> GetByIdAsync(Guid id)
    {
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message == null) return null;

        // Load users in batch to prevent N+1 queries
        var userIds = new[] { message.SenderId, message.ReceiverId };
        var users = await GetUsersByIdsAsync(userIds);
        
        var sender = users.FirstOrDefault(u => u.Id == message.SenderId);
        var receiver = users.FirstOrDefault(u => u.Id == message.ReceiverId);

        // Map AppIdentityUser to User for the navigation properties
        message.Sender = sender != null ? ConvertToUser(sender) : null;
        message.Receiver = receiver != null ? ConvertToUser(receiver) : null;

        return message;
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesBetweenUsersAsync(Guid user1Id, Guid user2Id, int page = 1, int pageSize = 50)
    {
        var messages = await _context.ChatMessages
            .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                       (m.SenderId == user2Id && m.ReceiverId == user1Id))
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (!messages.Any()) return messages;

        // Collect all unique user IDs to fetch in batch
        var userIds = messages
            .SelectMany(m => new[] { m.SenderId, m.ReceiverId })
            .Distinct()
            .ToArray();

        // Load users in batch to prevent N+1 queries
        var users = await GetUsersByIdsAsync(userIds);
        var userDictionary = users.ToDictionary(u => u.Id);

        // Map users to messages
        foreach (var message in messages)
        {
            if (userDictionary.TryGetValue(message.SenderId, out var sender))
                message.Sender = ConvertToUser(sender);
            
            if (userDictionary.TryGetValue(message.ReceiverId, out var receiver))
                message.Receiver = ConvertToUser(receiver);
        }

        return messages;
    }

    public async Task<ChatMessage> CreateAsync(ChatMessage message)
    {
        // Check if both sender and receiver exist in the AspNetUsers table before adding the message
        var userIds = new[] { message.SenderId, message.ReceiverId };
        var users = await GetUsersByIdsAsync(userIds);
        
        var sender = users.FirstOrDefault(u => u.Id == message.SenderId);
        var receiver = users.FirstOrDefault(u => u.Id == message.ReceiverId);

        if (sender == null)
        {
            throw new ArgumentException($"Sender with ID {message.SenderId} does not exist.");
        }

        if (receiver == null)
        {
            throw new ArgumentException($"Receiver with ID {message.ReceiverId} does not exist.");
        }

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        // Return the message with mapped users
        message.Sender = ConvertToUser(sender);
        message.Receiver = ConvertToUser(receiver);

        return message;
    }

    public async Task<IEnumerable<ChatMessage>> GetUserRecentMessagesAsync(Guid userId, int count = 20)
    {
        var messages = await _context.ChatMessages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync();

        if (!messages.Any()) return messages;

        // Collect all unique user IDs to fetch in batch
        var userIds = messages
            .SelectMany(m => new[] { m.SenderId, m.ReceiverId })
            .Distinct()
            .ToArray();

        // Load users in batch to prevent N+1 queries
        var users = await GetUsersByIdsAsync(userIds);
        var userDictionary = users.ToDictionary(u => u.Id);

        // Map users to messages
        foreach (var message in messages)
        {
            if (userDictionary.TryGetValue(message.SenderId, out var sender))
                message.Sender = ConvertToUser(sender);
            
            if (userDictionary.TryGetValue(message.ReceiverId, out var receiver))
                message.Receiver = ConvertToUser(receiver);
        }

        return messages;
    }

    public async Task<int> GetUnreadMessageCountAsync(Guid userId)
    {
        return await _context.ChatMessages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
    }

    private async Task<List<AppIdentityUser>> GetUsersByIdsAsync(Guid[] userIds)
    {
        if (userIds == null || userIds.Length == 0)
            return new List<AppIdentityUser>();

        var users = new List<AppIdentityUser>();
        var missingUserIds = new List<Guid>();

        // Check cache first for each user
        foreach (var userId in userIds.Distinct())
        {
            var cacheKey = $"identityuser_{userId}";
            if (_cache.TryGetValue(cacheKey, out AppIdentityUser? cachedUser) && cachedUser != null)
            {
                users.Add(cachedUser);
            }
            else
            {
                missingUserIds.Add(userId);
            }
        }

        // Fetch missing users from database
        if (missingUserIds.Any())
        {
            var dbUsers = await _userManager.Users
                .Where(u => missingUserIds.Contains(u.Id))
                .ToListAsync();

            // Cache the newly fetched users
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            };

            foreach (var user in dbUsers)
            {
                var cacheKey = $"identityuser_{user.Id}";
                _cache.Set(cacheKey, user, cacheOptions);
                users.Add(user);
            }
        }

        return users;
    }

    private static User ConvertToUser(AppIdentityUser identityUser)
    {
        return new User
        {
            Id = identityUser.Id,
            UserName = identityUser.UserName ?? string.Empty,
            Email = identityUser.Email ?? string.Empty,
            AvatarUrl = identityUser.AvatarUrl,
            CreatedAt = identityUser.CreatedAt,
            IsOnline = identityUser.IsOnline
        };
    }
}