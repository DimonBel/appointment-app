namespace IdentityApp.Domain.Entity;

/// <summary>
/// Doctor/Professional profile information
/// </summary>
public class DoctorProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public string? Qualifications { get; set; }
    public int YearsOfExperience { get; set; }
    public string? Services { get; set; } // JSON array of services offered
    public string? WorkingHours { get; set; } // JSON object with schedule
    public decimal? ConsultationFee { get; set; }
    public string? Languages { get; set; } // JSON array of languages spoken
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsAvailableForAppointments { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual AppIdentityUser User { get; set; } = null!;
}
