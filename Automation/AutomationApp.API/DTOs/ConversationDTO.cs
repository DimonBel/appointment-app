namespace AutomationApp.API.DTOs;

/// <summary>
/// DTO representing an AI conversation
/// </summary>
public class ConversationDTO
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string State { get; set; } = string.Empty;
    public string? DetectedIntent { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; }
}