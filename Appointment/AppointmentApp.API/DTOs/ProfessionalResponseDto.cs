namespace AppointmentApp.API.DTOs;

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
}

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}

public class AvailabilityResponseDto
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public int DayOfWeek { get; set; }
    public required string StartTime { get; set; }
    public required string EndTime { get; set; }
    public int ScheduleType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}