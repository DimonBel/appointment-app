using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Entity;

public class Order
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid? DomainConfigurationId { get; set; }
    public DomainType DomainType { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Requested;
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? DeclineReason { get; set; }
    public string? ApprovalReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? PreOrderDataId { get; set; }

    // Navigation properties
    public AppIdentityUser? Client { get; set; }
    public AppIdentityUser? Professional { get; set; }
    public DomainConfiguration? DomainConfiguration { get; set; }
    public PreOrderData? PreOrderData { get; set; }
    public ICollection<OrderHistory> OrderHistory { get; set; } = new List<OrderHistory>();
}