using System.ComponentModel.DataAnnotations;

namespace AppointmentApp.API.DTOs;

public class CreateOrderDto
{
    [Required]
    public Guid ProfessionalId { get; set; }

    [Required]
    public DateTime ScheduledDateTime { get; set; }

    [Required]
    [Range(15, 480)]
    public int DurationMinutes { get; set; } = 60;

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public Guid? DomainConfigurationId { get; set; }

    public Dictionary<string, string>? PreOrderData { get; set; }
}