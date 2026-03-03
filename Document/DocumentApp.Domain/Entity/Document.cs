using System.ComponentModel.DataAnnotations;
using DocumentApp.Domain.Enums;

namespace DocumentApp.Domain.Entity;

/// <summary>
/// Represents a document stored in MinIO
/// Supports various document types with access control
/// </summary>
public class Document
{
    /// <summary>
    /// Unique identifier for the document
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the file in storage
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Original filename uploaded by user
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type of the file
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    [Required]
    public long FileSize { get; set; }

    /// <summary>
    /// Path to the file in MinIO storage
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string MinioPath { get; set; } = string.Empty;

    /// <summary>
    /// MinIO bucket name where file is stored
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MinioBucket { get; set; } = string.Empty;

    /// <summary>
    /// Type of document (Avatar, Booking, Profile, etc.)
    /// </summary>
    [Required]
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Type of entity this document is linked to
    /// </summary>
    [Required]
    public LinkedEntityType LinkedEntityType { get; set; } = LinkedEntityType.None;

    /// <summary>
    /// ID of the linked entity
    /// </summary>
    public Guid? LinkedEntityId { get; set; }

    /// <summary>
    /// ID of the user who owns the document
    /// </summary>
    [Required]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Name of the document owner
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Whether the document is soft-deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp when document was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when document was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Access control entries for this document
    /// </summary>
    public ICollection<DocumentAccess> AccessControls { get; set; } = new List<DocumentAccess>();
}