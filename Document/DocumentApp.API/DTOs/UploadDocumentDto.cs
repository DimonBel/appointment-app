using DocumentApp.Domain.Enums;

namespace DocumentApp.API.DTOs;

public class UploadDocumentDto
{
    public DocumentType DocumentType { get; set; }
    public LinkedEntityType? LinkedEntityType { get; set; }
    public Guid? LinkedEntityId { get; set; }
}