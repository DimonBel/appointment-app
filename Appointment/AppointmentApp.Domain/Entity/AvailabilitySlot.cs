namespace AppointmentApp.Domain.Entity;

public class AvailabilitySlot
{
    public Guid Id { get; set; }
    public Guid AvailabilityId { get; set; }
    public DateTime SlotDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Availability? Availability { get; set; }
    public Order? Order { get; set; }
}