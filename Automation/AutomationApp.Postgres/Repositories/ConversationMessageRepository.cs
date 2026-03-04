using AutomationApp.Domain.Entity;
using AutomationApp.Repository.Interfaces;
using AutomationApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AutomationApp.Postgres.Repositories;

/// <summary>
/// Repository for managing conversation message entities
/// </summary>
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