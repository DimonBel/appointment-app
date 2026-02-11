using ChatApp.Domain.Entity;

namespace ChatApp.Domain.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, User? User)> RegisterAsync(string email, string password, string userName);
    Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password, bool rememberMe);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
}

public interface IChatService
{
    Task<ChatMessage> SendMessageAsync(Guid senderId, Guid receiverId, string content);
    Task<IEnumerable<ChatMessage>> GetMessagesBetweenUsersAsync(Guid user1Id, Guid user2Id, int page = 1, int pageSize = 50);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(Guid id);
    Task<IEnumerable<ChatMessage>> GetUserRecentMessagesAsync(Guid userId, int count = 20);
    Task<IEnumerable<User>> SearchUsersAsync(string query);
}