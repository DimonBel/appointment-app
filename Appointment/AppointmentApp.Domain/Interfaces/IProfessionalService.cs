using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Domain.Interfaces;

public interface IProfessionalService
{
    Task<Professional> CreateProfessionalAsync(Guid userId, string? title = null, string? qualifications = null, string? specialization = null);
    Task<Professional?> GetProfessionalByIdAsync(Guid professionalId);
    Task<Professional?> GetProfessionalByUserIdAsync(Guid userId);
    Task<IEnumerable<Professional>> GetAllProfessionalsAsync(bool onlyAvailable = true, int page = 1, int pageSize = 20);
    Task<Professional> UpdateProfessionalAsync(Guid professionalId, string? title = null, string? qualifications = null, string? specialization = null, decimal? hourlyRate = null, int? experienceYears = null, string? bio = null);
    Task<bool> SetProfessionalAvailabilityAsync(Guid professionalId, bool isAvailable);
    Task<bool> DeleteProfessionalAsync(Guid professionalId);
}