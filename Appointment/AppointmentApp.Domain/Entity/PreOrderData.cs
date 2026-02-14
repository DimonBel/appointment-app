namespace AppointmentApp.Domain.Entity;

public class PreOrderData
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ClientId { get; set; }
    public Dictionary<string, string> DataFields { get; set; } = new Dictionary<string, string>();
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Order? Order { get; set; }
    public AppIdentityUser? Client { get; set; }
}