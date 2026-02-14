using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Entity;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace IdentityApp.Service.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppIdentityUser> _userManager;

    public UserService(UserManager<AppIdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles.ToList());
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles.ToList());
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles.ToList());
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(MapToUserDto(user, roles.ToList()));
        }

        return userDtos;
    }

    public async Task<bool> UpdateUserAsync(Guid userId, UserDto userDto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.FirstName = userDto.FirstName;
        user.LastName = userDto.LastName;
        user.AvatarUrl = userDto.AvatarUrl;
        user.PhoneNumber = userDto.PhoneNumber;
        user.IsActive = userDto.IsActive;
        user.IsOnline = userDto.IsOnline;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> SetUserOnlineStatusAsync(Guid userId, bool isOnline)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.IsOnline = isOnline;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    private static UserDto MapToUserDto(AppIdentityUser user, List<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            IsOnline = user.IsOnline,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles
        };
    }
}
