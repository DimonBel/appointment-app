using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Entity;

public class DomainConfiguration
{
    public Guid Id { get; set; }
    public DomainType DomainType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DefaultDurationMinutes { get; set; } = 60;
    public Dictionary<string, string>? RequiredFields { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}