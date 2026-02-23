using DocumentApp.Domain.Enums;

namespace DocumentApp.API.DTOs;

public class DocumentResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DocumentType DocumentType { get; set; }
    public LinkedEntityType LinkedEntityType { get; set; }
    public Guid? LinkedEntityId { get; set; }
    public Guid OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set;
    }
    public string DownloadUrl { get; set; } = string.Empty;
}