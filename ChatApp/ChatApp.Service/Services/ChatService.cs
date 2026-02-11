using ChatApp.Domain.Entity;
using ChatApp.Domain.Interfaces;
using ChatApp.Repository.Interfaces;

namespace ChatApp.Service.Services;

public class ChatService : IChatService
{
    private readonly IChatMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;

    public ChatService(IChatMessageRepository messageRepository, IUserRepository userRepository)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
    }

    public async Task<ChatMessage> SendMessageAsync(Guid senderId, Guid receiverId, string content)
    {
        // The repository will validate that both sender and receiver exist
        var message = new ChatMessage
        {
            Content = content,
            SenderId = senderId,
            ReceiverId = receiverId,
            CreatedAt = DateTime.UtcNow
        };

        var createdMessage = await _messageRepository.CreateAsync(message);

        return createdMessage;
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesBetweenUsersAsync(Guid user1Id, Guid user2Id, int page = 1, int pageSize = 50)
    {
        return await _messageRepository.GetMessagesBetweenUsersAsync(user1Id, user2Id, page, pageSize);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ChatMessage>> GetUserRecentMessagesAsync(Guid userId, int count = 20)
    {
        return await _messageRepository.GetUserRecentMessagesAsync(userId, count);
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string query)
    {
        var allUsers = await _userRepository.GetAllAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            return allUsers;
        }

        var lowerQuery = query.ToLowerInvariant();
        return allUsers.Where(user =>
            user.UserName.ToLowerInvariant().Contains(lowerQuery) ||
            user.Email.ToLowerInvariant().Contains(lowerQuery));
    }
}