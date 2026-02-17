using Microsoft.EntityFrameworkCore;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Postgres.Data;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Postgres.Repositories;

public class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationDbContext _context;

    public NotificationTemplateRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationTemplate> CreateAsync(NotificationTemplate template)
    {
        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<NotificationTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.NotificationTemplates.FindAsync(id);
    }

    public async Task<NotificationTemplate?> GetByKeyAsync(string key)
    {
        return await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Key == key && t.IsActive);
    }

    public async Task<IEnumerable<NotificationTemplate>> GetAllAsync()
    {
        return await _context.NotificationTemplates
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationTemplate>> GetByTypeAsync(NotificationType type)
    {
        return await _context.NotificationTemplates
            .Where(t => t.Type == type && t.IsActive)
            .ToListAsync();
    }

    public async Task<NotificationTemplate> UpdateAsync(NotificationTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        _context.NotificationTemplates.Update(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task DeleteAsync(Guid id)
    {
        var template = await _context.NotificationTemplates.FindAsync(id);
        if (template != null)
        {
            _context.NotificationTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }
}
