using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Entity;

/// <summary>
/// Represents a professional's availability rule
/// Defines when a professional is available to take appointments
/// Supports regular, one-time, and recurring schedules
/// </summary>
public class Availability
{
    /// <summary>
    /// Unique identifier for the availability rule
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the professional this availability belongs to
    /// </summary>
    public Guid ProfessionalId { get; set; }

    /// <summary>
    /// Day of the week this availability applies to
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Start time of the availability window
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the availability window
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Type of schedule (Regular, OneTime, Recurring)
    /// </summary>
    public ScheduleType ScheduleType { get; set; }

    /// <summary>
    /// Optional start date for time-bound schedules
    /// Null means applies indefinitely from creation
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional end date for time-bound schedules
    /// Null means applies indefinitely
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether this availability rule is currently active
    /// Can be toggled without deleting the rule
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when this availability rule was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this availability rule was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The professional who owns this availability
    /// </summary>
    public Professional? Professional { get; set; }

    /// <summary>
    /// Generated time slots based on this availability rule
    /// </summary>
    public ICollection<AvailabilitySlot> Slots { get; set; } = new List<AvailabilitySlot>();
}