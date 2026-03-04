namespace DocumentApp.API.DTOs;

/// <summary>
/// DTO for line items in booking documents
/// </summary>
public class BookingDocumentLineItemDto
{
    public decimal Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}