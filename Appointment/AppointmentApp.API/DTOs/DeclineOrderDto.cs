using System.ComponentModel.DataAnnotations;

namespace AppointmentApp.API.DTOs;

public class DeclineOrderDto
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}