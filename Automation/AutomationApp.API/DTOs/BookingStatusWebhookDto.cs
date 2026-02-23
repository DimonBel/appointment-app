namespace AutomationApp.API.DTOs;

public class BookingStatusWebhookDto
{
    public Guid OrderId { get; set; }
    public Guid ClientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public string? Title { get; set; }
    public DateTime UpdatedAt { get; set; }
}