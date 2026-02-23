using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;
using DocumentApp.Postgres.Data;
using DocumentApp.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DocumentApp.Postgres.Repositories;

public class DocumentAccessRepository : IDocumentAccessRepository
{
    private readonly DocumentDbContext _context;

    public DocumentAccessRepository(DocumentDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentAccess?> GetByDocumentAndUserAsync(Guid documentId, Guid userId)
    {
        return await _context.DocumentAccesses
            .FirstOrDefaultAsync(da => da.DocumentId == documentId && da.UserId == userId);
    }

    public async Task<IEnumerable<DocumentAccess>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentAccesses
            .Where(da => da.DocumentId == documentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentAccess>> GetByUserIdAsync(Guid userId)
    {
        return await _context.DocumentAccesses
            .Where(da => da.UserId == userId)
            .ToListAsync();
    }

    public async Task<DocumentAccess> CreateAsync(DocumentAccess access)
    {
        _context.DocumentAccesses.Add(access);
        await _context.SaveChangesAsync();
        return access;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var access = await _context.DocumentAccesses.FindAsync(id);
        if (access == null) return false;

        _context.DocumentAccesses.Remove(access);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasAccessAsync(Guid documentId, Guid userId, AccessControlType requiredAccess)
    {
        var access = await _context.DocumentAccesses
            .FirstOrDefaultAsync(da => da.DocumentId == documentId && da.UserId == userId);

        if (access == null) return false;

        return access.AccessType >= requiredAccess;
    }
}