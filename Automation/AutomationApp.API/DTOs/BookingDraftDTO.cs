namespace AutomationApp.API.DTOs;

/// <summary>
/// DTO representing a booking draft in progress
/// </summary>
public class BookingDraftDTO
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? ProfessionalId { get; set; }
    public string? ServiceType { get; set; }
    public DateTime? PreferredDateTime { get; set; }
    public string? ClientNotes { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? FinalOrderId { get; set; }
}