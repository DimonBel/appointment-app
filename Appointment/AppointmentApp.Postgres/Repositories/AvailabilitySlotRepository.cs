using AppointmentApp.Domain.Entity;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Repositories;

public class AvailabilitySlotRepository : IAvailabilitySlotRepository
{
    private readonly AppointmentDbContext _context;

    public AvailabilitySlotRepository(AppointmentDbContext context)
    {
        _context = context;
    }

    public async Task<AvailabilitySlot?> GetByIdAsync(Guid id)
    {
        return await _context.AvailabilitySlots
            .Include(s => s.Availability)
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<AvailabilitySlot> CreateAsync(AvailabilitySlot slot)
    {
        _context.AvailabilitySlots.Add(slot);
        await _context.SaveChangesAsync();
        return slot;
    }

    public async Task<AvailabilitySlot> UpdateAsync(AvailabilitySlot slot)
    {
        slot.UpdatedAt = DateTime.UtcNow;
        _context.AvailabilitySlots.Update(slot);
        await _context.SaveChangesAsync();
        return slot;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var slot = await _context.AvailabilitySlots.FindAsync(id);
        if (slot == null) return false;
        _context.AvailabilitySlots.Remove(slot);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AvailabilitySlot>> GetByAvailabilityIdAsync(Guid availabilityId)
    {
        return await _context.AvailabilitySlots
            .Where(s => s.AvailabilityId == availabilityId)
            .OrderBy(s => s.SlotDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<AvailabilitySlot>> GetAvailableSlotsAsync(Guid professionalId, DateTime date)
    {
        return await _context.AvailabilitySlots
            .Include(s => s.Availability)
            .Where(s => s.Availability.ProfessionalId == professionalId
                        && s.SlotDate.Date == date.Date
                        && s.IsAvailable)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<AvailabilitySlot?> GetSlotByDateTimeAsync(Guid professionalId, DateTime dateTime)
    {
        var date = dateTime.Date;
        var time = dateTime.TimeOfDay;

        return await _context.AvailabilitySlots
            .Include(s => s.Availability)
            .Where(s => s.Availability.ProfessionalId == professionalId
                        && s.SlotDate.Date == date
                        && s.StartTime <= time
                        && s.EndTime > time)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.AvailabilitySlots.AnyAsync(s => s.Id == id);
    }

    public async Task<bool> IsSlotAvailableAsync(Guid professionalId, DateTime dateTime, int durationMinutes)
    {
        var slot = await GetSlotByDateTimeAsync(professionalId, dateTime);
        if (slot == null || !slot.IsAvailable)
        {
            return false;
        }

        var requestedEndTime = dateTime.TimeOfDay.Add(TimeSpan.FromMinutes(durationMinutes));
        return requestedEndTime <= slot.EndTime;
    }
}