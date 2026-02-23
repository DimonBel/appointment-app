using System.ComponentModel.DataAnnotations;
using DocumentApp.Domain.Enums;

namespace DocumentApp.Domain.Entity;

public class DocumentAccess
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid DocumentId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public string? UserName { get; set; }

    [Required]
    public AccessControlType AccessType { get; set; }

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public Guid? GrantedBy { get; set; }

    // Navigation property
    public Document Document { get; set; } = null!;
}