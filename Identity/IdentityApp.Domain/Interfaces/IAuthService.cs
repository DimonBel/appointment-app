using IdentityApp.Domain.DTOs;

namespace IdentityApp.Domain.Interfaces;

/// <summary>
/// Service interface for authentication operations
/// Handles user registration, login, email confirmation, token management, and user lookup
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account
    /// Creates user account with email, username, and password
    /// Sends email confirmation if configured
    /// </summary>
    /// <param name="model">User registration data</param>
    /// <returns>Tuple with success status, message, and auth response with tokens if successful</returns>
    Task<(bool Success, string Message, AuthResponseDto? Response)> RegisterAsync(RegisterDto model);

    /// <summary>
    /// Authenticates a user with email and password
    /// Validates credentials and returns access and refresh tokens
    /// </summary>
    /// <param name="model">User login credentials</param>
    /// <returns>Tuple with success status, message, and auth response with tokens if successful</returns>
    Task<(bool Success, string Message, AuthResponseDto? Response)> LoginAsync(LoginDto model);

    /// <summary>
    /// Confirms a user's email address
    /// Validates the email confirmation token
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="token">Email confirmation token</param>
    /// <returns>Tuple with success status, message, and auth response if successful</returns>
    Task<(bool Success, string Message, AuthResponseDto? Response)> ConfirmEmailAsync(Guid userId, string token);

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// Allows users to maintain authenticated session without re-login
    /// </summary>
    /// <param name="model">Refresh token data</param>
    /// <returns>Tuple with success status, message, and auth response with new tokens if successful</returns>
    Task<(bool Success, string Message, AuthResponseDto? Response)> RefreshTokenAsync(RefreshTokenDto model);

    /// <summary>
    /// Revokes a refresh token
    /// Invalidates the token, forcing re-login
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>True if token revoked successfully, false otherwise</returns>
    Task<bool> RevokeTokenAsync(string userId);

    /// <summary>
    /// Validates a JWT access token
    /// Checks signature, expiration, and validity
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Retrieves a user by their ID
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>User data if found, null otherwise</returns>
    Task<UserDto?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Retrieves a user by their email address
    /// </summary>
    /// <param name="email">Email address of the user</param>
    /// <returns>User data if found, null otherwise</returns>
    Task<UserDto?> GetUserByEmailAsync(string email);
}