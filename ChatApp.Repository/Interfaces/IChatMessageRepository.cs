using ChatApp.Domain.Entity;

namespace ChatApp.Repository.Interfaces;

public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid id);
    Task<IEnumerable<ChatMessage>> GetMessagesBetweenUsersAsync(Guid user1Id, Guid user2Id, int page = 1, int pageSize = 50);
    Task<ChatMessage> CreateAsync(ChatMessage message);
    Task<IEnumerable<ChatMessage>> GetUserRecentMessagesAsync(Guid userId, int count = 20);
    Task<int> GetUnreadMessageCountAsync(Guid userId);
}