namespace AppointmentApp.API.DTOs;

/// <summary>
/// DTO representing professional availability schedule
/// </summary>
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