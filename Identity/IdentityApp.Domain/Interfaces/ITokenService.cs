using IdentityApp.Domain.Entity;

namespace IdentityApp.Domain.Interfaces;

/// <summary>
/// Service interface for JWT token management
/// Handles access token generation, refresh token management, and token validation
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for a user
    /// Includes user claims and role information
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <param name="roles">List of user roles</param>
    /// <returns>JWT access token string</returns>
    string GenerateAccessToken(AppIdentityUser user, IList<string> roles);

    /// <summary>
    /// Generates a secure random refresh token
    /// Refresh tokens are longer-lived and used to obtain new access tokens
    /// </summary>
    /// <returns>Random refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Retrieves a refresh token from storage
    /// </summary>
    /// <param name="token">Refresh token string</param>
    /// <returns>Refresh token entity if found, null otherwise</returns>
    Task<RefreshToken?> GetRefreshTokenAsync(string token);

    /// <summary>
    /// Saves a refresh token to storage
    /// </summary>
    /// <param name="refreshToken">Refresh token entity to save</param>
    /// <returns>True if saved successfully, false otherwise</returns>
    Task<bool> SaveRefreshTokenAsync(RefreshToken refreshToken);

    /// <summary>
    /// Revokes (invalidates) a refresh token
    /// </summary>
    /// <param name="token">Refresh token to revoke</param>
    /// <returns>True if revoked successfully, false otherwise</returns>
    Task<bool> RevokeRefreshTokenAsync(string token);

    /// <summary>
    /// Revokes all refresh tokens for a user
    /// Forces logout from all devices
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>True if all tokens revoked successfully, false otherwise</returns>
    Task<bool> RevokeAllUserTokensAsync(Guid userId);

    /// <summary>
    /// Validates a JWT access token
    /// Returns user ID if valid, null otherwise
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User ID if valid, null otherwise</returns>
    string? ValidateToken(string token);
}