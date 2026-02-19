using AppointmentApp.Domain.Entity;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Repositories;

public class AvailabilityRepository : IAvailabilityRepository
{
    private readonly AppointmentDbContext _context;

    public AvailabilityRepository(AppointmentDbContext context)
    {
        _context = context;
    }

    public async Task<Availability?> GetByIdAsync(Guid id)
    {
        return await _context.Availabilities
            .Include(a => a.Professional)
            .Include(a => a.Slots)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Availability>> GetAllAsync()
    {
        return await _context.Availabilities
            .Include(a => a.Professional)
                .ThenInclude(p => p.User)
            .Include(a => a.Slots)
            .Where(a => a.IsActive)
            .OrderBy(a => a.ProfessionalId)
            .ThenBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<Availability> CreateAsync(Availability availability)
    {
        _context.Availabilities.Add(availability);
        await _context.SaveChangesAsync();
        return availability;
    }

    public async Task<Availability> UpdateAsync(Availability availability)
    {
        availability.UpdatedAt = DateTime.UtcNow;
        _context.Availabilities.Update(availability);
        await _context.SaveChangesAsync();
        return availability;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var availability = await _context.Availabilities.FindAsync(id);
        if (availability == null) return false;
        _context.Availabilities.Remove(availability);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Availability>> GetByProfessionalAsync(Guid professionalId)
    {
        return await _context.Availabilities
            .Include(a => a.Slots)
            .Where(a => a.ProfessionalId == professionalId && a.IsActive)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Availabilities.AnyAsync(a => a.Id == id);
    }
}