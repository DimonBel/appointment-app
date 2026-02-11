using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp.Domain.Entity;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; } = false;

    [NotMapped]
    public ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
    
    [NotMapped]
    public ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
}