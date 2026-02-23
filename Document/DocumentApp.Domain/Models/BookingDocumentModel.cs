namespace DocumentApp.Domain.Models;

public class BookingDocumentModel
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

    public List<BookingDocumentLineItem> LineItems { get; set; } = [];
    public string AdditionalInformation { get; set; } = string.Empty;

    public decimal TaxRate { get; set; } = 0.075m;

    public decimal Subtotal => LineItems.Sum(x => x.Amount);
    public decimal TaxAmount => decimal.Round(Subtotal * TaxRate, 2);
    public decimal TotalAmount => Subtotal + TaxAmount;
}

public class BookingDocumentLineItem
{
    public decimal Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Amount => decimal.Round(Quantity * UnitPrice, 2);
}
