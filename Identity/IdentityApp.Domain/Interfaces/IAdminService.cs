using IdentityApp.Domain.DTOs;

namespace IdentityApp.Domain.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<AdminUserDto>> GetAllUsersWithDetailsAsync();
    Task<UserStatisticsDto> GetUserStatisticsAsync();
    Task<UserDto?> CreateUserAsync(CreateUserDto createUserDto);
    Task<bool> UpdateUserAsync(Guid userId, UserDto userDto);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> ToggleUserActiveStatusAsync(Guid userId);
    Task<bool> AssignRoleAsync(Guid userId, string roleName);
    Task<bool> RemoveRoleAsync(Guid userId, string roleName);
    Task<bool> ResetUserPasswordAsync(Guid userId, string newPassword);
}