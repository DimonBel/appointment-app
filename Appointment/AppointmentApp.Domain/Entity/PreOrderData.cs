namespace AppointmentApp.Domain.Entity;

/// <summary>
/// Represents data collected before order confirmation
/// Stores dynamic form data based on domain requirements
/// Used to gather client-specific information (medical history, legal case details, etc.)
/// </summary>
public class PreOrderData
{
    /// <summary>
    /// Unique identifier for the pre-order data entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the associated order
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// ID of the client who provided the data
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Dynamic field-value pairs of collected data
    /// Keys match required fields from DomainConfiguration
    /// </summary>
    public Dictionary<string, string> DataFields { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether all required data has been collected
    /// True when ready for order processing
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Timestamp when data collection started
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when data was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The associated order
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// The client who provided the data
    /// </summary>
    public AppIdentityUser? Client { get; set; }
}