using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;

namespace DocumentApp.Repository.Interfaces;

public interface IDocumentAccessRepository
{
    Task<DocumentAccess?> GetByDocumentAndUserAsync(Guid documentId, Guid userId);
    Task<IEnumerable<DocumentAccess>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<DocumentAccess>> GetByUserIdAsync(Guid userId);
    Task<DocumentAccess> CreateAsync(DocumentAccess access);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> HasAccessAsync(Guid documentId, Guid userId, AccessControlType requiredAccess);
}