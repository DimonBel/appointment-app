namespace AutomationApp.Domain.Entity;

public class ProfessionalInfo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string? Specialization { get; set; }
    public string? Qualifications { get; set; }
    public string? Bio { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool IsAvailable { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}