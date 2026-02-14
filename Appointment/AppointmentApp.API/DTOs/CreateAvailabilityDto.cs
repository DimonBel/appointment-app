using AppointmentApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentApp.API.DTOs;

public class CreateAvailabilityDto
{
    [Required]
    public Guid ProfessionalId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Required]
    public ScheduleType ScheduleType { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}