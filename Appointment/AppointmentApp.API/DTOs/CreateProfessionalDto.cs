using System.ComponentModel.DataAnnotations;

namespace AppointmentApp.API.DTOs;

public class CreateProfessionalDto
{
    [Required]
    public Guid UserId { get; set; }

    [MaxLength(100)]
    public string? Title { get; set; }

    [MaxLength(500)]
    public string? Qualifications { get; set; }

    [MaxLength(200)]
    public string? Specialization { get; set; }

    [Range(0, 10000)]
    public decimal? HourlyRate { get; set; }

    [Range(0, 60)]
    public int? ExperienceYears { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }
}