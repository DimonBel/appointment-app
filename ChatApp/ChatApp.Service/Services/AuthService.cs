using ChatApp.Domain.Entity;
using ChatApp.Domain.Interfaces;
using ChatApp.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Service.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly SignInManager<AppIdentityUser> _signInManager;
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        UserManager<AppIdentityUser> userManager,
        SignInManager<AppIdentityUser> signInManager,
        IUserRepository userRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<(bool Success, string Message, User? User)> RegisterAsync(string email, string password, string userName)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return (false, "A user with this email already exists.", null);
        }

        var identityUser = new AppIdentityUser
        {
            UserName = userName,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(identityUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Registration failed: {errors}", null);
        }

        await _signInManager.SignInAsync(identityUser, isPersistent: false);

        var user = new User
        {
            Id = identityUser.Id,
            UserName = identityUser.UserName ?? string.Empty,
            Email = identityUser.Email ?? string.Empty,
            AvatarUrl = identityUser.AvatarUrl,
            CreatedAt = identityUser.CreatedAt,
            IsOnline = identityUser.IsOnline
        };

        return (true, "Registration successful", user);
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password, bool rememberMe)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return (false, "Invalid email or password.", null);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            password,
            rememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return (false, "Invalid email or password.", null);
        }

        var appUser = await _userRepository.GetByIdAsync(user.Id);
        if (appUser == null)
        {
            return (false, "User not found.", null);
        }

        appUser.IsOnline = true;
        await _userRepository.UpdateAsync(appUser);

        return (true, "Login successful", appUser);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();

        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user != null)
            {
                user.IsOnline = false;
                await _userRepository.UpdateAsync(user);
            }
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);
        return user;
    }

    private Guid? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
        {
            return null;
        }

        var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}