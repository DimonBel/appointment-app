using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Entity;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace IdentityApp.Service.Services;

public class AdminService : IAdminService
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly RoleManager<AppIdentityRole> _roleManager;

    public AdminService(UserManager<AppIdentityUser> userManager, RoleManager<AppIdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<AdminUserDto>> GetAllUsersWithDetailsAsync()
    {
        var users = _userManager.Users.ToList();
        var userDtos = new List<AdminUserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(MapToAdminUserDto(user, roles.ToList()));
        }

        return userDtos.OrderByDescending(u => u.CreatedAt);
    }

    public async Task<UserStatisticsDto> GetUserStatisticsAsync()
    {
        var users = _userManager.Users.ToList();
        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var usersByRole = new Dictionary<string, int>();
        var allRoles = _roleManager.Roles.ToList();

        foreach (var role in allRoles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            usersByRole[role.Name!] = usersInRole.Count;
        }

        return new UserStatisticsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            InactiveUsers = users.Count(u => !u.IsActive),
            OnlineUsers = users.Count(u => u.IsOnline),
            UsersByRole = usersByRole,
            UsersRegisteredToday = users.Count(u => u.CreatedAt >= todayStart),
            UsersRegisteredThisWeek = users.Count(u => u.CreatedAt >= weekStart),
            UsersRegisteredThisMonth = users.Count(u => u.CreatedAt >= monthStart)
        };
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserDto createUserDto)
    {
        var user = new AppIdentityUser
        {
            Email = createUserDto.Email,
            UserName = createUserDto.UserName,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            PhoneNumber = createUserDto.PhoneNumber,
            IsActive = createUserDto.IsActive,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true // Admin-created users are auto-confirmed
        };

        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded) return null;

        // Assign role
        if (!await _roleManager.RoleExistsAsync(createUserDto.Role))
        {
            await _roleManager.CreateAsync(new AppIdentityRole { Name = createUserDto.Role });
        }

        await _userManager.AddToRoleAsync(user, createUserDto.Role);

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles.ToList());
    }

    public async Task<bool> UpdateUserAsync(Guid userId, UserDto userDto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        if (!string.IsNullOrWhiteSpace(userDto.UserName) &&
            !string.Equals(user.UserName, userDto.UserName, StringComparison.Ordinal))
        {
            var setUserNameResult = await _userManager.SetUserNameAsync(user, userDto.UserName);
            if (!setUserNameResult.Succeeded) return false;
        }

        if (!string.IsNullOrWhiteSpace(userDto.Email) &&
            !string.Equals(user.Email, userDto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, userDto.Email);
            if (!setEmailResult.Succeeded) return false;
        }

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

    public async Task<bool> ToggleUserActiveStatusAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> AssignRoleAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new AppIdentityRole { Name = roleName });
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> ResetUserPasswordAsync(Guid userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
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

    private static AdminUserDto MapToAdminUserDto(AppIdentityUser user, List<string> roles)
    {
        return new AdminUserDto
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
            Roles = roles,
            EmailConfirmed = user.EmailConfirmed ? "Confirmed" : "Not Confirmed"
        };
    }
}