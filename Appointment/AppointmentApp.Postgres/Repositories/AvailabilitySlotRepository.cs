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

    public async Task<IEnumerable<AvailabilitySlot>> GetSlotsByDateAsync(Guid professionalId, DateTime date)
    {
        var dateUtc = NormalizeToUtc(date);

        return await _context.AvailabilitySlots
            .Include(s => s.Availability)
            .Where(s => s.Availability != null
                        && s.Availability.ProfessionalId == professionalId
                        && s.SlotDate.Date == dateUtc.Date)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<AvailabilitySlot>> GetAvailableSlotsAsync(Guid professionalId, DateTime date)
    {
        var dateUtc = NormalizeToUtc(date);

        return await _context.AvailabilitySlots
            .Include(s => s.Availability)
            .Where(s => s.Availability != null
                        && s.Availability.ProfessionalId == professionalId
                        && s.SlotDate.Date == dateUtc.Date
                        && s.IsAvailable)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<AvailabilitySlot?> GetSlotByDateTimeAsync(Guid professionalId, DateTime dateTime)
    {
        var dateTimeUtc = NormalizeToUtc(dateTime);
        var date = dateTimeUtc.Date;
        var time = dateTimeUtc.TimeOfDay;

        return await _context.AvailabilitySlots
            .Include(s => s.Availability)
            .Where(s => s.Availability != null
                        && s.Availability.ProfessionalId == professionalId
                        && s.SlotDate.Date == date
                        && s.StartTime == time)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.AvailabilitySlots.AnyAsync(s => s.Id == id);
    }

    public async Task<bool> IsSlotAvailableAsync(Guid professionalId, DateTime dateTime, int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            return false;
        }

        var dateTimeUtc = NormalizeToUtc(dateTime);
        var requestedStartTime = dateTimeUtc.TimeOfDay;
        var requestedEndTime = requestedStartTime.Add(TimeSpan.FromMinutes(durationMinutes));

        var daySlots = (await GetSlotsByDateAsync(professionalId, dateTimeUtc.Date))
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.StartTime)
            .ToList();

        if (daySlots.Count == 0)
        {
            return false;
        }

        var slotAtStart = daySlots.FirstOrDefault(s => s.StartTime == requestedStartTime);
        if (slotAtStart == null)
        {
            return false;
        }

        var overlappingSlots = daySlots
            .Where(s => s.StartTime >= requestedStartTime && s.StartTime < requestedEndTime)
            .OrderBy(s => s.StartTime)
            .ToList();

        if (overlappingSlots.Count == 0)
        {
            return false;
        }

        for (var index = 1; index < overlappingSlots.Count; index++)
        {
            if (overlappingSlots[index - 1].EndTime != overlappingSlots[index].StartTime)
            {
                return false;
            }
        }

        return overlappingSlots.Last().EndTime >= requestedEndTime;
    }

    private static DateTime NormalizeToUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }

        if (dateTime.Kind == DateTimeKind.Local)
        {
            return dateTime.ToUniversalTime();
        }

        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }
}