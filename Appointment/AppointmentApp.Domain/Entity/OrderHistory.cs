using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Entity;

public class OrderHistory
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public OrderStatus PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation properties
    public Order? Order { get; set; }
    public AppIdentityUser? ChangedByUser { get; set; }
}