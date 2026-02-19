using System.ComponentModel.DataAnnotations;
using DocumentApp.Domain.Enums;

namespace DocumentApp.Domain.Entity;

public class Document
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [Required]
    [MaxLength(500)]
    public string MinioPath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string MinioBucket { get; set; } = string.Empty;

    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    public LinkedEntityType LinkedEntityType { get; set; } = LinkedEntityType.None;

    public Guid? LinkedEntityId { get; set; }

    [Required]
    public Guid OwnerId { get; set; }

    public string? OwnerName { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<DocumentAccess> AccessControls { get; set; } = new List<DocumentAccess>();
}