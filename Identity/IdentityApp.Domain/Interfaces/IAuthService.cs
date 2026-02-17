using IdentityApp.Domain.DTOs;

namespace IdentityApp.Domain.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, AuthResponseDto? Response)> RegisterAsync(RegisterDto model);
    Task<(bool Success, string Message, AuthResponseDto? Response)> LoginAsync(LoginDto model);
    Task<(bool Success, string Message)> ConfirmEmailAsync(Guid userId, string token);
    Task<(bool Success, string Message, AuthResponseDto? Response)> RefreshTokenAsync(RefreshTokenDto model);
    Task<bool> RevokeTokenAsync(string userId);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
}
