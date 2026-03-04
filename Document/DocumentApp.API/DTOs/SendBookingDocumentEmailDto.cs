namespace DocumentApp.API.DTOs;

/// <summary>
/// DTO for sending booking document via email
/// </summary>
public class SendBookingDocumentEmailDto
{
    public GenerateBookingDocumentDto Booking { get; set; } = new();
    public string RecipientEmail { get; set; } = string.Empty;
}