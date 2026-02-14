using Microsoft.AspNetCore.Identity;

namespace IdentityApp.Domain.Entity;

/// <summary>
/// Extended Identity User with additional properties
/// </summary>
public class AppIdentityUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public new string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOnline { get; set; } = false;

    // Navigation property for refresh tokens
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
