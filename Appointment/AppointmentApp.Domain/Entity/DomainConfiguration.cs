using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Entity;

/// <summary>
/// Represents configuration for a service domain
/// Defines business rules and default settings for different service types (Medical, Legal, Consulting, etc.)
/// Controls appointment duration, required data fields, and domain-specific parameters
/// </summary>
public class DomainConfiguration
{
    /// <summary>
    /// Unique identifier for the domain configuration
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of domain (Medical, Legal, Consulting, etc.)
    /// </summary>
    public DomainType DomainType { get; set; }

    /// <summary>
    /// Display name of the domain
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the domain
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this domain configuration is currently active
    /// Inactive domains cannot be used for new orders
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Default appointment duration in minutes
    /// Used as fallback when not specified
    /// </summary>
    public int DefaultDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Required fields for pre-order data collection
    /// Key is field name, value is display label
    /// </summary>
    public Dictionary<string, string>? RequiredFields { get; set; }

    /// <summary>
    /// Timestamp when this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this configuration was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}