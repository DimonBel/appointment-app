using Microsoft.AspNetCore.Identity;

namespace AppointmentApp.Domain.Entity;

public class AppIdentityRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}