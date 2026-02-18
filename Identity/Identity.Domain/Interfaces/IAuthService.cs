using Identity.Domain.Entity;

namespace Identity.Domain.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, AppIdentityUser? User)> RegisterAsync(string email, string password, string userName, string? firstName, string? lastName);
    Task<(bool Success, string Message, AppIdentityUser? User)> LoginAsync(string email, string password, bool rememberMe);
    Task LogoutAsync();
    Task<AppIdentityUser?> GetCurrentUserAsync();
}
