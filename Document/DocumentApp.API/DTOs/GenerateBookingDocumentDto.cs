using DocumentApp.Domain.Models;

namespace DocumentApp.API.DTOs;

/// <summary>
/// DTO for generating a booking confirmation document
/// </summary>
public class GenerateBookingDocumentDto
{
    public Guid OrderId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? DoctorId { get; set; }
    public string FacilityName { get; set; } = "Healthcare Hub";
    public string FacilityAddress { get; set; } = string.Empty;
    public string FacilityPhone { get; set; } = string.Empty;
    public string FacilityEmail { get; set; } = string.Empty;
    public string FacilityWebsite { get; set; } = string.Empty;
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDateUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public string PatientName { get; set; } = "Patient";
    public string PatientEmail { get; set; } = string.Empty;
    public string DoctorName { get; set; } = "Doctor";
    public DateTime ScheduledDateTimeUtc { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public decimal TaxRate { get; set; } = 0.075m;
    public string AdditionalInformation { get; set; } = string.Empty;
    public List<BookingDocumentLineItemDto> LineItems { get; set; } = [];

    public BookingDocumentModel ToModel()
    {
        return new BookingDocumentModel
        {
            OrderId = OrderId,
            ClientId = ClientId,
            DoctorId = DoctorId,
            FacilityName = FacilityName,
            FacilityAddress = FacilityAddress,
            FacilityPhone = FacilityPhone,
            FacilityEmail = FacilityEmail,
            FacilityWebsite = FacilityWebsite,
            BookingNumber = BookingNumber,
            BookingDateUtc = BookingDateUtc,
            Status = Status,
            PatientName = PatientName,
            PatientEmail = PatientEmail,
            DoctorName = DoctorName,
            ScheduledDateTimeUtc = ScheduledDateTimeUtc,
            DurationMinutes = DurationMinutes,
            TaxRate = TaxRate,
            AdditionalInformation = AdditionalInformation,
            LineItems = LineItems.Select(x => new BookingDocumentLineItem
            {
                Quantity = x.Quantity,
                Description = x.Description,
                UnitPrice = x.UnitPrice
            }).ToList()
        };
    }
}