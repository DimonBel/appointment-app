namespace ChatApp.Domain.Entity;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public bool IsRead { get; set; } = false;

    // Navigation properties - will be populated by repository
    public User? Sender { get; set; }
    public User? Receiver { get; set; }
}