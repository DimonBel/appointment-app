using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Repository.Interfaces;

public interface IAvailabilityRepository
{
    Task<Availability?> GetByIdAsync(Guid id);
    Task<IEnumerable<Availability>> GetAllAsync();
    Task<Availability> CreateAsync(Availability availability);
    Task<Availability> UpdateAsync(Availability availability);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Availability>> GetByProfessionalAsync(Guid professionalId);
    Task<bool> ExistsAsync(Guid id);
}