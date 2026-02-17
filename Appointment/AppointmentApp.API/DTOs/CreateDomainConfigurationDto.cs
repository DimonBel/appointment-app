using AppointmentApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentApp.API.DTOs;

public class CreateDomainConfigurationDto
{
    [Required]
    public DomainType DomainType { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(15, 480)]
    public int DefaultDurationMinutes { get; set; } = 60;

    public Dictionary<string, string>? RequiredFields { get; set; }
}