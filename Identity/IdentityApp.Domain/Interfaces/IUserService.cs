using IdentityApp.Domain.DTOs;

namespace IdentityApp.Domain.Interfaces;

/// <summary>
/// Service interface for user account management
/// Handles user CRUD operations and user information retrieval
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their ID
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>User data if found, null otherwise</returns>
    Task<UserDto?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Retrieves a user by their email address
    /// </summary>
    /// <param name="email">Email address of the user</param>
    /// <returns>User data if found, null otherwise</returns>
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by their username
    /// </summary>
    /// <param name="username">Username of the user</param>
    /// <returns>User data if found, null otherwise</returns>
    Task<UserDto?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Retrieves all users in the system
    /// </summary>
    /// <returns>Collection of all users</returns>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Searches for users by name, email, or username
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <returns>Collection of matching users</returns>
    Task<IEnumerable<UserDto>> SearchUsersAsync(string query);

    /// <summary>
    /// Updates an existing user's information
    /// </summary>
    /// <param name="userId">ID of the user to update</param>
    /// <param name="userDto">Updated user data</param>
    /// <returns>True if updated successfully, false otherwise</returns>
    Task<bool> UpdateUserAsync(Guid userId, UserDto userDto);

    /// <summary>
    /// Deletes a user account
    /// Soft deletes user, preserving data for audit
    /// </summary>
    /// <param name="userId">ID of the user to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Sets a user's online status
    /// Tracks whether user is currently active in the system
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="isOnline">True if online, false if offline</param>
    /// <returns>True if status updated successfully, false otherwise</returns>
    Task<bool> SetUserOnlineStatusAsync(Guid userId, bool isOnline);
}