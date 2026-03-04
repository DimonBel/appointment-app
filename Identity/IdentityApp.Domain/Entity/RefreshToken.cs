namespace IdentityApp.Domain.Entity;

/// <summary>
/// Refresh Token for JWT authentication
/// Used to obtain new access tokens without re-authentication
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the user this token belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The refresh token string
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// ID of the JWT this token was issued for
    /// Used for token revocation
    /// </summary>
    public string JwtId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this token has been used
    /// Tokens can only be used once
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Whether this token has been revoked
    /// Revoked tokens cannot be used
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Timestamp when token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    // Navigation property

    /// <summary>
    /// The user this token belongs to
    /// </summary>
    public virtual AppIdentityUser? User { get; set; }
}