namespace DocumentApp.API.DTOs;

/// <summary>
/// DTO response after generating a booking document
/// </summary>
public class BookingDocumentResponseDto
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
}