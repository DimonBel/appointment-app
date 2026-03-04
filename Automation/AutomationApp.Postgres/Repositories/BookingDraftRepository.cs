using AutomationApp.Domain.Entity;
using AutomationApp.Repository.Interfaces;
using AutomationApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AutomationApp.Postgres.Repositories;

/// <summary>
/// Repository for managing booking draft entities
/// </summary>
public class BookingDraftRepository : IBookingDraftRepository
{
    private readonly AutomationDbContext _context;

    public BookingDraftRepository(AutomationDbContext context)
    {
        _context = context;
    }

    public async Task<BookingDraft?> GetByIdAsync(Guid id)
    {
        return await _context.BookingDrafts.FindAsync(id);
    }

    public async Task<BookingDraft?> GetByConversationIdAsync(Guid conversationId)
    {
        return await _context.BookingDrafts
            .FirstOrDefaultAsync(b => b.ConversationId == conversationId);
    }

    public async Task<IEnumerable<BookingDraft>> GetByUserIdAsync(Guid userId)
    {
        return await _context.BookingDrafts
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<BookingDraft> AddAsync(BookingDraft draft)
    {
        _context.BookingDrafts.Add(draft);
        await _context.SaveChangesAsync();
        return draft;
    }

    public async Task<BookingDraft> UpdateAsync(BookingDraft draft)
    {
        _context.BookingDrafts.Update(draft);
        await _context.SaveChangesAsync();
        return draft;
    }

    public async Task DeleteAsync(Guid id)
    {
        var draft = await _context.BookingDrafts.FindAsync(id);
        if (draft != null)
        {
            _context.BookingDrafts.Remove(draft);
            await _context.SaveChangesAsync();
        }
    }
}