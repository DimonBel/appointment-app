using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Repository.Interfaces;

public interface IProfessionalRepository
{
    Task<Professional?> GetByIdAsync(Guid id);
    Task<Professional?> GetByUserIdAsync(Guid userId);
    Task<Professional> CreateAsync(Professional professional);
    Task<Professional> UpdateAsync(Professional professional);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Professional>> GetAllAsync(bool onlyAvailable = true, int page = 1, int pageSize = 20);
    Task<bool> ExistsAsync(Guid id);
}