using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;

namespace DocumentApp.Repository.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id);
    Task<IEnumerable<Document>> GetAllAsync(int page = 1, int pageSize = 50, DocumentType? documentType = null);
    Task<IEnumerable<Document>> GetByOwnerIdAsync(Guid ownerId, int page = 1, int pageSize = 20);
    Task<IEnumerable<Document>> GetByLinkedEntityAsync(LinkedEntityType entityType, Guid entityId);
    Task<Document> CreateAsync(Document document);
    Task<Document> UpdateAsync(Document document);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<int> GetTotalCountAsync(DocumentType? documentType = null);
}