namespace AutomationApp.Domain.Entity;

/// <summary>
/// Information about a professional for booking selection
/// Lightweight data transfer object for conversational booking
/// </summary>
public class ProfessionalInfo
{
    /// <summary>
    /// Unique identifier for the professional
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the user account
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Professional title (e.g., Dr., Attorney)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Area of specialization
    /// </summary>
    public string? Specialization { get; set; }

    /// <summary>
    /// Qualifications and credentials
    /// </summary>
    public string? Qualifications { get; set; }

    /// <summary>
    /// Professional biography
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Hourly rate for services
    /// </summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>
    /// Whether the professional is available for new bookings
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// First name of the professional
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name of the professional
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string? Email { get; set; }
}