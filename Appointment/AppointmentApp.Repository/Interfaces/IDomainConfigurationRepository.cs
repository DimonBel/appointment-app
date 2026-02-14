using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Repository.Interfaces;

public interface IDomainConfigurationRepository
{
    Task<DomainConfiguration?> GetByIdAsync(Guid id);
    Task<DomainConfiguration> CreateAsync(DomainConfiguration domainConfiguration);
    Task<DomainConfiguration> UpdateAsync(DomainConfiguration domainConfiguration);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<DomainConfiguration>> GetAllAsync(bool onlyActive = true);
    Task<bool> ExistsAsync(Guid id);
}