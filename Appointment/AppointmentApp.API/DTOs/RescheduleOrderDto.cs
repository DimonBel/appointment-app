using System.ComponentModel.DataAnnotations;

namespace AppointmentApp.API.DTOs;

public class RescheduleOrderDto
{
    [Required]
    public DateTime NewScheduledDateTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
