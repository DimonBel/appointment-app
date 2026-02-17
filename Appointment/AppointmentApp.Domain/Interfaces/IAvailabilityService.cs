using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Interfaces;

public interface IAvailabilityService
{
    Task<Availability> CreateAvailabilityAsync(Guid professionalId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, ScheduleType scheduleType, DateTime? startDate = null, DateTime? endDate = null);
    Task<Availability?> GetAvailabilityByIdAsync(Guid availabilityId);
    Task<IEnumerable<Availability>> GetAllAvailabilitiesAsync();
    Task<IEnumerable<Availability>> GetAvailabilitiesByProfessionalAsync(Guid professionalId);
    Task<Availability> UpdateAvailabilityAsync(Guid availabilityId, DayOfWeek? dayOfWeek = null, TimeSpan? startTime = null, TimeSpan? endTime = null, DateTime? endDate = null);
    Task<bool> DeleteAvailabilityAsync(Guid availabilityId);
    Task<IEnumerable<AvailabilitySlot>> GetSlotsByDateAsync(Guid professionalId, DateTime date);
    Task<IEnumerable<AvailabilitySlot>> GetAvailableSlotsAsync(Guid professionalId, DateTime date);
    Task<bool> IsSlotAvailableAsync(Guid professionalId, DateTime dateTime, int durationMinutes);
    Task<IEnumerable<AvailabilitySlot>> GenerateSlotsForDateAsync(Guid professionalId, DateTime date);
}