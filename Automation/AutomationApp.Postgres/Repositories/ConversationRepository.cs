using AutomationApp.Domain.Entity;
using AutomationApp.Repository.Interfaces;
using AutomationApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AutomationApp.Postgres.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AutomationDbContext _context;

    public ConversationRepository(AutomationDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Conversation?> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync()
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .ToListAsync();
    }

    public async Task<Conversation> AddAsync(Conversation conversation)
    {
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<Conversation> UpdateAsync(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task DeleteAsync(Guid id)
    {
        var conversation = await _context.Conversations.FindAsync(id);
        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();
        }
    }
}

public class ConversationMessageRepository : IConversationMessageRepository
{
    private readonly AutomationDbContext _context;

    public ConversationMessageRepository(AutomationDbContext context)
    {
        _context = context;
    }

    public async Task<ConversationMessage?> GetByIdAsync(Guid id)
    {
        return await _context.ConversationMessages.FindAsync(id);
    }

    public async Task<IEnumerable<ConversationMessage>> GetByConversationIdAsync(Guid conversationId)
    {
        return await _context.ConversationMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<ConversationMessage> AddAsync(ConversationMessage message)
    {
        _context.ConversationMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<IEnumerable<ConversationMessage>> AddRangeAsync(IEnumerable<ConversationMessage> messages)
    {
        _context.ConversationMessages.AddRange(messages);
        await _context.SaveChangesAsync();
        return messages;
    }
}

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