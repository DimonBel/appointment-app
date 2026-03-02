using AutomationApp.Domain.Entity;
using AutomationApp.Repository.Interfaces;
using AutomationApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AutomationApp.Postgres.Repositories;

/// <summary>
/// Repository for managing conversation entities
/// </summary>
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
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Conversation?> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Conversations
            .Include(c => c.Messages
                .OrderBy(m => m.SentAt)
                .Take(1))
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastActivityAt ?? c.StartedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync()
    {
        return await _context.Conversations
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