namespace AppointmentApp.Domain.Entity;

/// <summary>
/// Represents a specific time slot available for booking
/// Generated from availability rules and represents concrete bookable time
/// Can be booked by exactly one order
/// </summary>
public class AvailabilitySlot
{
    /// <summary>
    /// Unique identifier for the time slot
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the availability rule that generated this slot
    /// </summary>
    public Guid AvailabilityId { get; set; }

    /// <summary>
    /// Date of the time slot
    /// </summary>
    public DateTime SlotDate { get; set; }

    /// <summary>
    /// Start time of the slot
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the slot
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Whether this slot is available for booking
    /// False when booked or blocked
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Timestamp when this slot was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this slot was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The availability rule that generated this slot
    /// </summary>
    public Availability? Availability { get; set; }

    /// <summary>
    /// The order that booked this slot, if any
    /// </summary>
    public Order? Order { get; set; }
}