using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;
using DocumentApp.Domain.Interfaces;
using DocumentApp.Repository.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocumentApp.Service.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;
    private readonly IMinioDocumentStorageService _storageService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository,
        IMinioDocumentStorageService storageService,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Document> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSize,
        Guid ownerId,
        string ownerName,
        DocumentType documentType,
        LinkedEntityType linkedEntityType = LinkedEntityType.None,
        Guid? linkedEntityId = null)
    {
        try
        {
            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            var bucketName = "documents";

            // Upload to MinIO
            await _storageService.UploadFileAsync(fileStream, fileSize, fileName, bucketName, contentType);

            // Create document record
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                OriginalFileName = originalFileName,
                ContentType = contentType,
                FileSize = fileSize,
                MinioPath = fileName,
                MinioBucket = bucketName,
                DocumentType = documentType,
                LinkedEntityType = linkedEntityType,
                LinkedEntityId = linkedEntityId,
                OwnerId = ownerId,
                OwnerName = ownerName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _documentRepository.CreateAsync(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document: {FileName}", originalFileName);
            throw;
        }
    }

    public async Task<Document?> GetDocumentByIdAsync(Guid id)
    {
        return await _documentRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Document>> GetDocumentsByOwnerAsync(Guid ownerId, int page = 1, int pageSize = 20)
    {
        return await _documentRepository.GetByOwnerIdAsync(ownerId, page, pageSize);
    }

    public async Task<IEnumerable<Document>> GetDocumentsByLinkedEntityAsync(LinkedEntityType entityType, Guid entityId)
    {
        return await _documentRepository.GetByLinkedEntityAsync(entityType, entityId);
    }

    public async Task<IEnumerable<Document>> GetAllDocumentsAsync(int page = 1, int pageSize = 50, DocumentType? documentType = null)
    {
        return await _documentRepository.GetAllAsync(page, pageSize, documentType);
    }

    public async Task<Stream> DownloadDocumentAsync(Guid id, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            throw new FileNotFoundException($"Document not found: {id}");
        }

        // Check access: owner always has access, others need explicit access
        if (document.OwnerId != userId)
        {
            var hasAccess = await _documentAccessRepository.HasAccessAsync(id, userId, AccessControlType.Download);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You don't have permission to download this document");
            }
        }

        return await _storageService.DownloadFileAsync(document.MinioPath, document.MinioBucket);
    }

    public async Task<bool> DeleteDocumentAsync(Guid id, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            return false;
        }

        // Only owner can delete
        if (document.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this document");
        }

        // Delete from MinIO (optional - could keep for audit)
        await _storageService.DeleteFileAsync(document.MinioPath, document.MinioBucket);

        // Soft delete from database
        return await _documentRepository.DeleteAsync(id);
    }

    public async Task<bool> GrantAccessAsync(Guid documentId, Guid userId, AccessControlType accessType, Guid grantedBy)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            return false;
        }

        // Only owner can grant access
        if (document.OwnerId != grantedBy)
        {
            throw new UnauthorizedAccessException("Only the owner can grant access to this document");
        }

        // Check if access already exists
        var existingAccess = await _documentAccessRepository.GetByDocumentAndUserAsync(documentId, userId);
        if (existingAccess != null)
        {
            // Update existing access
            existingAccess.AccessType = accessType;
            existingAccess.GrantedBy = grantedBy;
            return true;
        }

        // Create new access
        var access = new DocumentAccess
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = userId,
            AccessType = accessType,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = grantedBy
        };

        await _documentAccessRepository.CreateAsync(access);
        return true;
    }

    public async Task<bool> RevokeAccessAsync(Guid documentId, Guid userId)
    {
        var access = await _documentAccessRepository.GetByDocumentAndUserAsync(documentId, userId);
        if (access == null)
        {
            return false;
        }

        return await _documentAccessRepository.DeleteAsync(access.Id);
    }

    public async Task<bool> HasAccessAsync(Guid documentId, Guid userId, AccessControlType requiredAccess)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            return false;
        }

        // Owner always has full access
        if (document.OwnerId == userId)
        {
            return true;
        }

        return await _documentAccessRepository.HasAccessAsync(documentId, userId, requiredAccess);
    }

    public async Task<Document?> UpdateDocumentMetadataAsync(Guid id, string? fileName = null)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(fileName))
        {
            document.FileName = fileName;
        }

        return await _documentRepository.UpdateAsync(document);
    }

    public async Task UpdateLinkedEntityAsync(Guid id, LinkedEntityType linkedEntityType, Guid linkedEntityId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            throw new FileNotFoundException($"Document not found: {id}");
        }

        document.LinkedEntityType = linkedEntityType;
        document.LinkedEntityId = linkedEntityId;
        document.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(document);
    }
}