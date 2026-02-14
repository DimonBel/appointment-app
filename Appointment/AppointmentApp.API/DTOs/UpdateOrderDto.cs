using System.ComponentModel.DataAnnotations;

namespace AppointmentApp.API.DTOs;

public class UpdateOrderDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}