using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Repository.Interfaces;

public interface IAvailabilitySlotRepository
{
    Task<AvailabilitySlot?> GetByIdAsync(Guid id);
    Task<AvailabilitySlot> CreateAsync(AvailabilitySlot slot);
    Task<AvailabilitySlot> UpdateAsync(AvailabilitySlot slot);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<AvailabilitySlot>> GetByAvailabilityIdAsync(Guid availabilityId);
    Task<IEnumerable<AvailabilitySlot>> GetSlotsByDateAsync(Guid professionalId, DateTime date);
    Task<IEnumerable<AvailabilitySlot>> GetAvailableSlotsAsync(Guid professionalId, DateTime date);
    Task<AvailabilitySlot?> GetSlotByDateTimeAsync(Guid professionalId, DateTime dateTime);
    Task<bool> IsSlotAvailableAsync(Guid professionalId, DateTime dateTime, int durationMinutes);
    Task<bool> ExistsAsync(Guid id);
}