using Microsoft.EntityFrameworkCore;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Postgres.Data;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Postgres.Repositories;

public class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly NotificationDbContext _context;

    public NotificationPreferenceRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NotificationPreference>> GetByUserIdAsync(Guid userId)
    {
        return await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.NotificationType)
            .ToListAsync();
    }

    public async Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, NotificationType type)
    {
        return await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type);
    }

    public async Task<NotificationPreference> CreateAsync(NotificationPreference preference)
    {
        _context.NotificationPreferences.Add(preference);
        await _context.SaveChangesAsync();
        return preference;
    }

    public async Task<NotificationPreference> UpdateAsync(NotificationPreference preference)
    {
        preference.UpdatedAt = DateTime.UtcNow;
        _context.NotificationPreferences.Update(preference);
        await _context.SaveChangesAsync();
        return preference;
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
