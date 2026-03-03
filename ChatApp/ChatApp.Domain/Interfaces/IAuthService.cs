using ChatApp.Domain.Entity;

namespace ChatApp.Domain.Interfaces;

/// <summary>
/// Service interface for user authentication in the Chat application
/// Handles user registration, login, logout, and session management
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account
    /// Validates email uniqueness and password requirements
    /// </summary>
    /// <param name="email">User's email address (must be unique)</param>
    /// <param name="password">User's password (must meet complexity requirements)</param>
    /// <param name="userName">User's display name</param>
    /// <returns>Tuple with success status, message, and user object if successful</returns>
    Task<(bool Success, string Message, User? User)> RegisterAsync(string email, string password, string userName);

    /// <summary>
    /// Authenticates a user with email and password
    /// Validates credentials and creates user session
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password</param>
    /// <param name="rememberMe">Whether to persist session across browser restarts</param>
    /// <returns>Tuple with success status, message, and user object if successful</returns>
    Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password, bool rememberMe);

    /// <summary>
    /// Logs out the current user
    /// Clears session and authentication tokens
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Retrieves the currently authenticated user
    /// </summary>
    /// <returns>Current user if authenticated, null otherwise</returns>
    Task<User?> GetCurrentUserAsync();
}

/// <summary>
/// Service interface for chat messaging functionality
/// Handles sending messages, retrieving conversations, and user management
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Sends a chat message from one user to another
    /// Stores message and marks timestamp
    /// </summary>
    /// <param name="senderId">ID of the user sending the message</param>
    /// <param name="receiverId">ID of the user receiving the message</param>
    /// <param name="content">Message content</param>
    /// <returns>Sent chat message</returns>
    Task<ChatMessage> SendMessageAsync(Guid senderId, Guid receiverId, string content);

    /// <summary>
    /// Retrieves all messages between two users
    /// Supports pagination for large conversations
    /// </summary>
    /// <param name="user1Id">ID of the first user</param>
    /// <param name="user2Id">ID of the second user</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of messages per page</param>
    /// <returns>Collection of chat messages between the users</returns>
    Task<IEnumerable<ChatMessage>> GetMessagesBetweenUsersAsync(Guid user1Id, Guid user2Id, int page = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves all users in the system
    /// </summary>
    /// <returns>Collection of all users</returns>
    Task<IEnumerable<User>> GetAllUsersAsync();

    /// <summary>
    /// Retrieves a user by their ID
    /// </summary>
    /// <param name="id">ID of the user</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Retrieves recent messages for a user
    /// Useful for displaying conversation previews
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="count">Number of recent messages to retrieve</param>
    /// <returns>Collection of recent chat messages</returns>
    Task<IEnumerable<ChatMessage>> GetUserRecentMessagesAsync(Guid userId, int count = 20);

    /// <summary>
    /// Searches for users by name or email
    /// Useful for finding users to start conversations with
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <returns>Collection of matching users</returns>
    Task<IEnumerable<User>> SearchUsersAsync(string query);
}