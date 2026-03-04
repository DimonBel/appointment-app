using IdentityApp.Domain.DTOs;

namespace IdentityApp.Domain.Interfaces;

/// <summary>
/// Service interface for administrative user management
/// Handles user CRUD operations, role assignment, and system statistics
/// Requires Admin role for access
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Retrieves all users with detailed information
    /// Includes roles, status, and profile details
    /// </summary>
    /// <returns>Collection of all admin user details</returns>
    Task<IEnumerable<AdminUserDto>> GetAllUsersWithDetailsAsync();

    /// <summary>
    /// Retrieves user statistics for the system
    /// Includes total users, active users, role distribution, etc.
    /// </summary>
    /// <returns>User statistics data</returns>
    Task<UserStatisticsDto> GetUserStatisticsAsync();

    /// <summary>
    /// Creates a new user (admin operation)
    /// Allows admins to create user accounts directly
    /// </summary>
    /// <param name="createUserDto">User creation details</param>
    /// <returns>Created user if successful, null otherwise</returns>
    Task<UserDto?> CreateUserAsync(CreateUserDto createUserDto);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="userId">ID of the user to update</param>
    /// <param name="userDto">Updated user details</param>
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
    /// Toggles a user's active status
    /// Activates or deactivates a user account
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>True if status toggled successfully, false otherwise</returns>
    Task<bool> ToggleUserActiveStatusAsync(Guid userId);

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="roleName">Name of the role to assign</param>
    /// <returns>True if role assigned successfully, false otherwise</returns>
    Task<bool> AssignRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="roleName">Name of the role to remove</param>
    /// <returns>True if role removed successfully, false otherwise</returns>
    Task<bool> RemoveRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Resets a user's password
    /// Admin operation to override user password
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="newPassword">New password to set</param>
    /// <returns>True if password reset successfully, false otherwise</returns>
    Task<bool> ResetUserPasswordAsync(Guid userId, string newPassword);
}