using Microsoft.AspNetCore.Identity;

namespace IdentityApp.Domain.Entity;

/// <summary>
/// Extended Identity Role
/// </summary>
public class AppIdentityRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
