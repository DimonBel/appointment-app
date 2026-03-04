namespace AppointmentApp.API.DTOs;

/// <summary>
/// DTO representing a professional with user information
/// </summary>
public class ProfessionalResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string? Qualifications { get; set; }
    public string? Specialization { get; set; }
    public decimal? HourlyRate { get; set; }
    public int? ExperienceYears { get; set; }
    public string? Bio { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public UserInfoDto? User { get; set; }
    public List<AvailabilityResponseDto>? Availabilities { get; set; }
}