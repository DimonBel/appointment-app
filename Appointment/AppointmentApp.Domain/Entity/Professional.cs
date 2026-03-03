namespace AppointmentApp.Domain.Entity;

/// <summary>
/// Represents a professional who provides services and can be booked for appointments
/// Links a user account to professional capabilities and information
/// </summary>
public class Professional
{
    /// <summary>
    /// Unique identifier for the professional profile
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the associated user account
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Professional title (e.g., Dr., Attorney, Consultant)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Qualifications and credentials
    /// </summary>
    public string? Qualifications { get; set; }

    /// <summary>
    /// Area of specialization
    /// </summary>
    public string? Specialization { get; set; }

    /// <summary>
    /// Hourly rate for services
    /// Null if rate is not applicable or varies
    /// </summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>
    /// Years of professional experience
    /// </summary>
    public int? ExperienceYears { get; set; }

    /// <summary>
    /// Professional biography
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Whether the professional is available to accept new bookings
    /// Can be toggled to control availability
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Timestamp when this professional profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this professional profile was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The associated user account
    /// </summary>
    public AppIdentityUser? User { get; set; }

    /// <summary>
    /// Availability rules for this professional
    /// </summary>
    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
}