using Microsoft.AspNetCore.Identity;

namespace ChatApp.Domain.Entity;

public class AppIdentityUser : IdentityUser<Guid>
{
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; } = false;
}