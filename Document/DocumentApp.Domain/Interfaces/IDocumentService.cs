using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;

namespace DocumentApp.Domain.Interfaces;

public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSize,
        Guid ownerId,
        string ownerName,
        DocumentType documentType,
        LinkedEntityType linkedEntityType = LinkedEntityType.None,
        Guid? linkedEntityId = null);

    Task<Document?> GetDocumentByIdAsync(Guid id);
    Task<IEnumerable<Document>> GetDocumentsByOwnerAsync(Guid ownerId, int page = 1, int pageSize = 20);
    Task<IEnumerable<Document>> GetDocumentsByLinkedEntityAsync(LinkedEntityType entityType, Guid entityId);
    Task<IEnumerable<Document>> GetAllDocumentsAsync(int page = 1, int pageSize = 50, DocumentType? documentType = null);
    Task<Stream> DownloadDocumentAsync(Guid id, Guid userId);
    Task<bool> DeleteDocumentAsync(Guid id, Guid userId);
    Task<bool> GrantAccessAsync(Guid documentId, Guid userId, AccessControlType accessType, Guid grantedBy);
    Task<bool> RevokeAccessAsync(Guid documentId, Guid userId);
    Task<bool> HasAccessAsync(Guid documentId, Guid userId, AccessControlType requiredAccess);
    Task<Document?> UpdateDocumentMetadataAsync(Guid id, string? fileName = null);
    Task UpdateLinkedEntityAsync(Guid id, LinkedEntityType linkedEntityType, Guid linkedEntityId);
}