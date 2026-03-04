using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;

namespace DocumentApp.Domain.Interfaces;

/// <summary>
/// Service interface for document management with MinIO storage
/// Handles upload, download, deletion, access control, and metadata management
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Uploads a new document to MinIO storage
    /// Creates database record with metadata and access control
    /// </summary>
    /// <param name="fileStream">Stream containing file data</param>
    /// <param name="originalFileName">Original name of the file</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="fileSize">Size of the file in bytes</param>
    /// <param name="ownerId">ID of the user who owns the document</param>
    /// <param name="ownerName">Name of the document owner</param>
    /// <param name="documentType">Type of document (Avatar, Booking, Profile, etc.)</param>
    /// <param name="linkedEntityType">Optional type of entity this document is linked to</param>
    /// <param name="linkedEntityId">Optional ID of entity this document is linked to</param>
    /// <returns>Uploaded document with metadata and storage URL</returns>
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

    /// <summary>
    /// Retrieves a document by its ID
    /// </summary>
    /// <param name="id">ID of the document</param>
    /// <returns>Document if found, null otherwise</returns>
    Task<Document?> GetDocumentByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all documents owned by a specific user
    /// Supports pagination for large collections
    /// </summary>
    /// <param name="ownerId">ID of the owner</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Collection of documents owned by the user</returns>
    Task<IEnumerable<Document>> GetDocumentsByOwnerAsync(Guid ownerId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Retrieves all documents linked to a specific entity
    /// Useful for fetching documents associated with orders, profiles, etc.
    /// </summary>
    /// <param name="entityType">Type of linked entity</param>
    /// <param name="entityId">ID of the linked entity</param>
    /// <returns>Collection of linked documents</returns>
    Task<IEnumerable<Document>> GetDocumentsByLinkedEntityAsync(LinkedEntityType entityType, Guid entityId);

    /// <summary>
    /// Retrieves all documents with optional filtering and pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="documentType">Optional filter by document type</param>
    /// <returns>Collection of documents</returns>
    Task<IEnumerable<Document>> GetAllDocumentsAsync(int page = 1, int pageSize = 50, DocumentType? documentType = null);

    /// <summary>
    /// Downloads a document from MinIO storage
    /// Verifies access permissions before download
    /// </summary>
    /// <param name="id">ID of the document to download</param>
    /// <param name="userId">ID of the user requesting download</param>
    /// <param name="bypassAccessControl">If true, skips access control check (admin use only)</param>
    /// <returns>Stream containing the document data</returns>
    Task<Stream> DownloadDocumentAsync(Guid id, Guid userId, bool bypassAccessControl = false);

    /// <summary>
    /// Deletes a document from MinIO storage and database
    /// Verifies ownership before deletion
    /// </summary>
    /// <param name="id">ID of the document to delete</param>
    /// <param name="userId">ID of the user requesting deletion</param>
    /// <param name="bypassOwnershipCheck">If true, skips ownership check (admin use only)</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteDocumentAsync(Guid id, Guid userId, bool bypassOwnershipCheck = false);

    /// <summary>
    /// Grants access to a document for a user
    /// Allows shared access control
    /// </summary>
    /// <param name="documentId">ID of the document</param>
    /// <param name="userId">ID of the user to grant access to</param>
    /// <param name="accessType">Type of access to grant (Read, Write, Full)</param>
    /// <param name="grantedBy">ID of the user granting the access</param>
    /// <returns>True if access granted successfully, false otherwise</returns>
    Task<bool> GrantAccessAsync(Guid documentId, Guid userId, AccessControlType accessType, Guid grantedBy);

    /// <summary>
    /// Revokes access to a document from a user
    /// </summary>
    /// <param name="documentId">ID of the document</param>
    /// <param name="userId">ID of the user to revoke access from</param>
    /// <returns>True if access revoked successfully, false otherwise</returns>
    Task<bool> RevokeAccessAsync(Guid documentId, Guid userId);

    /// <summary>
    /// Checks if a user has access to a document
    /// </summary>
    /// <param name="documentId">ID of the document</param>
    /// <param name="userId">ID of the user</param>
    /// <param name="requiredAccess">Required access level</param>
    /// <returns>True if user has required access, false otherwise</returns>
    Task<bool> HasAccessAsync(Guid documentId, Guid userId, AccessControlType requiredAccess);

    /// <summary>
    /// Updates document metadata
    /// </summary>
    /// <param name="id">ID of the document</param>
    /// <param name="fileName">Optional new file name</param>
    /// <returns>Updated document</returns>
    Task<Document?> UpdateDocumentMetadataAsync(Guid id, string? fileName = null);

    /// <summary>
    /// Updates the linked entity for a document
    /// Associates or reassociates a document with an entity
    /// </summary>
    /// <param name="id">ID of the document</param>
    /// <param name="linkedEntityType">Type of linked entity</param>
    /// <param name="linkedEntityId">ID of linked entity</param>
    Task UpdateLinkedEntityAsync(Guid id, LinkedEntityType linkedEntityType, Guid linkedEntityId);
}