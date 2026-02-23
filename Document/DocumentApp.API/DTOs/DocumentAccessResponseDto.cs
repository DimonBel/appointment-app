using DocumentApp.Domain.Enums;

namespace DocumentApp.API.DTOs;

public class DocumentAccessResponseDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public AccessControlType AccessType { get; set; }
    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }
}