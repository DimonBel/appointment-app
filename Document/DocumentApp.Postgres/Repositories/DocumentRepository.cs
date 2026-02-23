using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;
using DocumentApp.Postgres.Data;
using DocumentApp.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DocumentApp.Postgres.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DocumentDbContext _context;

    public DocumentRepository(DocumentDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _context.Documents
            .Include(d => d.AccessControls)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<IEnumerable<Document>> GetAllAsync(int page = 1, int pageSize = 50, DocumentType? documentType = null)
    {
        var query = _context.Documents
            .Include(d => d.AccessControls)
            .Where(d => !d.IsDeleted);

        if (documentType.HasValue)
        {
            query = query.Where(d => d.DocumentType == documentType.Value);
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetByOwnerIdAsync(Guid ownerId, int page = 1, int pageSize = 20)
    {
        return await _context.Documents
            .Include(d => d.AccessControls)
            .Where(d => d.OwnerId == ownerId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetByLinkedEntityAsync(LinkedEntityType entityType, Guid entityId)
    {
        return await _context.Documents
            .Include(d => d.AccessControls)
            .Where(d => d.LinkedEntityType == entityType && d.LinkedEntityId == entityId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Document> CreateAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<Document> UpdateAsync(Document document)
    {
        document.UpdatedAt = DateTime.UtcNow;
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null) return false;

        document.IsDeleted = true;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Documents.AnyAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<int> GetTotalCountAsync(DocumentType? documentType = null)
    {
        var query = _context.Documents.Where(d => !d.IsDeleted);
        
        if (documentType.HasValue)
        {
            query = query.Where(d => d.DocumentType == documentType.Value);
        }

        return await query.CountAsync();
    }
}