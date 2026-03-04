namespace IdentityApp.Domain.Entity;

/// <summary>
/// Doctor/Professional profile information
/// Extends user account with professional details
/// </summary>
public class DoctorProfile
{
    /// <summary>
    /// Unique identifier for the profile
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the associated user account
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Medical specialty or professional focus area
    /// </summary>
    public string? Specialty { get; set; }

    /// <summary>
    /// Professional biography
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Qualifications and credentials
    /// </summary>
    public string? Qualifications { get; set; }

    /// <summary>
    /// Years of professional experience
    /// </summary>
    public int YearsOfExperience { get; set; }

    /// <summary>
    /// Services offered (JSON array)
    /// </summary>
    public string? Services { get; set; }

    /// <summary>
    /// Consultation fee
    /// </summary>
    public decimal? ConsultationFee { get; set; }

    /// <summary>
    /// Languages spoken (JSON array)
    /// </summary>
    public string? Languages { get; set; }

    /// <summary>
    /// Physical address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Whether the doctor is available for appointments
    /// </summary>
    public bool IsAvailableForAppointments { get; set; } = true;

    /// <summary>
    /// Timestamp when profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when profile was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation property

    /// <summary>
    /// The associated user account
    /// </summary>
    public virtual AppIdentityUser User { get; set; } = null!;
}