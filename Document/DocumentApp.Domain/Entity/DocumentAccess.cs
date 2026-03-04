using System.ComponentModel.DataAnnotations;
using DocumentApp.Domain.Enums;

namespace DocumentApp.Domain.Entity;

/// <summary>
/// Represents access control for a document
/// Defines which users have what level of access
/// </summary>
public class DocumentAccess
{
    /// <summary>
    /// Unique identifier for the access control entry
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the document this access control applies to
    /// </summary>
    [Required]
    public Guid DocumentId { get; set; }

    /// <summary>
    /// ID of the user who has access
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Name of the user
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Type of access granted (Read, Write, Full)
    /// </summary>
    [Required]
    public AccessControlType AccessType { get; set; }

    /// <summary>
    /// Timestamp when access was granted
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the user who granted this access
    /// </summary>
    public Guid? GrantedBy { get; set; }

    // Navigation property

    /// <summary>
    /// The document this access control applies to
    /// </summary>
    public Document Document { get; set; } = null!;
}