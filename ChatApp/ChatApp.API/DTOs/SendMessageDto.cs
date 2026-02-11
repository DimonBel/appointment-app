using System.ComponentModel.DataAnnotations;

namespace ChatApp.API.DTOs;

public class SendMessageDto
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public Guid ReceiverId { get; set; }
}