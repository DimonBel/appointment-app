using Microsoft.AspNetCore.Identity;

namespace AutomationApp.Domain.Entity;

public class AppIdentityRole : IdentityRole<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}