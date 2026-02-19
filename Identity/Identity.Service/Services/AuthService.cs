using Identity.Domain.Entity;
using Identity.Domain.Interfaces;
using Identity.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Identity.Service.Services;

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

    public async Task<(bool Success, string Message, AppIdentityUser? User)> RegisterAsync(string email, string password, string userName, string? firstName, string? lastName)
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
            FirstName = firstName,
            LastName = lastName,
            AvatarUrl = null,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(identityUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Registration failed: {errors}", null);
        }

        return (true, "Registration successful", identityUser);
    }

    public async Task<(bool Success, string Message, AppIdentityUser? User)> LoginAsync(string email, string password, bool rememberMe)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return (false, "Invalid email or password.", null);
        }

        if (!user.IsActive)
        {
            return (false, "User account is inactive.", null);
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

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return (true, "Login successful", user);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<AppIdentityUser?> GetCurrentUserAsync()
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
