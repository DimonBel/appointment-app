using IdentityApp.Domain.Entity;

namespace IdentityApp.Repository.Interfaces;

public interface IDoctorProfileRepository
{
    Task<DoctorProfile?> GetByIdAsync(Guid id);
    Task<DoctorProfile?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<DoctorProfile>> GetAllAsync();
    Task<IEnumerable<DoctorProfile>> GetBySpecialtyAsync(string specialty);
    Task<IEnumerable<DoctorProfile>> SearchAsync(string query);
    Task<DoctorProfile> CreateAsync(DoctorProfile profile);
    Task<DoctorProfile> UpdateAsync(DoctorProfile profile);
    Task DeleteAsync(Guid id);
}
