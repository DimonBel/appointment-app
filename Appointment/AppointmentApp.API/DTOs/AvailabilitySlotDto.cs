namespace AppointmentApp.API.DTOs;

public class AvailabilitySlotDto
{
    public Guid Id { get; set; }
    public Guid AvailabilityId { get; set; }
    public DateTime SlotDate { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
}