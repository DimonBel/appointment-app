using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Entity;

/// <summary>
/// Represents a historical record of order status changes
/// Provides audit trail for order lifecycle transitions
/// </summary>
public class OrderHistory
{
    /// <summary>
    /// Unique identifier for the history entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the order this history belongs to
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Previous status of the order before the change
    /// </summary>
    public OrderStatus PreviousStatus { get; set; }

    /// <summary>
    /// New status of the order after the change
    /// </summary>
    public OrderStatus NewStatus { get; set; }

    /// <summary>
    /// Reason for the status change
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// ID of the user who made the status change
    /// </summary>
    public Guid? ChangedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the status change occurred
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional additional notes about the change
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// The order this history entry belongs to
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// The user who made the status change
    /// </summary>
    public AppIdentityUser? ChangedByUser { get; set; }
}