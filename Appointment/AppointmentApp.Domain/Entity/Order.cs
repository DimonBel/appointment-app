using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Entity;

/// <summary>
/// Core appointment booking order entity supporting multiple domains (medical, legal, consulting)
/// Follows state machine: Requested → Approved/Declined → Completed/Cancelled
/// Maintains complete audit trail via OrderHistory navigation property
/// </summary>
public class Order
{
    /// <summary>
    /// Unique identifier for the order
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID of the client placing the order
    /// </summary>
    public Guid ClientId { get; set; }
    
    /// <summary>
    /// ID of the professional to book
    /// </summary>
    public Guid ProfessionalId { get; set; }
    
    /// <summary>
    /// Optional domain configuration ID for service type customization
    /// </summary>
    public Guid? DomainConfigurationId { get; set; }
    
    /// <summary>
    /// Domain type (Medical, Legal, Consulting, etc.)
    /// </summary>
    public DomainType DomainType { get; set; }
    
    /// <summary>
    /// Current order status following state machine rules
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Requested;
    
    /// <summary>
    /// Scheduled date and time for the appointment (UTC)
    /// </summary>
    public DateTime ScheduledDateTime { get; set; }
    
    /// <summary>
    /// Duration of the appointment in minutes
    /// </summary>
    public int DurationMinutes { get; set; }
    
    /// <summary>
    /// Optional title for the order
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Optional description for the order
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Optional notes about the order
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Reason for declining the order (populated when Status = Declined)
    /// </summary>
    public string? DeclineReason { get; set; }
    
    /// <summary>
    /// Reason for approving the order (populated when Status = Approved)
    /// </summary>
    public string? ApprovalReason { get; set; }
    
    /// <summary>
    /// Timestamp when the order was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when the order was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Timestamp when the order was completed (populated when Status = Completed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Optional reference to pre-order collected data
    /// </summary>
    public Guid? PreOrderDataId { get; set; }

    // Navigation properties
    public AppIdentityUser? Client { get; set; }
    public AppIdentityUser? Professional { get; set; }
    public DomainConfiguration? DomainConfiguration { get; set; }
    public PreOrderData? PreOrderData { get; set; }
    public ICollection<OrderHistory> OrderHistory { get; set; } = new List<OrderHistory>();
}