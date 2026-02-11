namespace ChatApp.API.DTOs;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public UserDto? Sender { get; set; }
    public UserDto? Receiver { get; set; }
}