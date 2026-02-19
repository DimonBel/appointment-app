using System.ComponentModel.DataAnnotations;

namespace IdentityApp.Domain.DTOs;

public record DoctorProfileDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public UserInfoDto? User { get; init; }
    public string? Specialty { get; init; }
    public string? Bio { get; init; }
    public string? Qualifications { get; init; }
    public int YearsOfExperience { get; init; }
    public List<string> Services { get; init; } = new();
    public decimal? ConsultationFee { get; init; }
    public List<string> Languages { get; init; } = new();
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public bool IsAvailableForAppointments { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record UserInfoDto
{
    public Guid Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? UserName { get; init; }
    public string? PhoneNumber { get; init; }
}

public record CreateDoctorProfileDto
{
    public string? Specialty { get; init; }
    public string? Bio { get; init; }
    public string? Qualifications { get; init; }
    public int YearsOfExperience { get; init; }
    public List<string> Services { get; init; } = new();
    public decimal? ConsultationFee { get; init; }
    public List<string> Languages { get; init; } = new();
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
}

public record UpdateDoctorProfileDto
{
    public string? Specialty { get; init; }
    public string? Bio { get; init; }
    public string? Qualifications { get; init; }
    public int YearsOfExperience { get; init; }
    public List<string> Services { get; init; } = new();
    public decimal? ConsultationFee { get; init; }
    public List<string> Languages { get; init; } = new();
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public bool? IsAvailableForAppointments { get; init; }
}
