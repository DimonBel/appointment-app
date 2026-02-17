using IdentityApp.Domain.DTOs;

namespace IdentityApp.Domain.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<IEnumerable<UserDto>> SearchUsersAsync(string query);
    Task<bool> UpdateUserAsync(Guid userId, UserDto userDto);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> SetUserOnlineStatusAsync(Guid userId, bool isOnline);
}
