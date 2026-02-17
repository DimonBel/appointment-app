using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Entity;

public class Availability
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public ScheduleType ScheduleType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Professional? Professional { get; set; }
    public ICollection<AvailabilitySlot> Slots { get; set; } = new List<AvailabilitySlot>();
}