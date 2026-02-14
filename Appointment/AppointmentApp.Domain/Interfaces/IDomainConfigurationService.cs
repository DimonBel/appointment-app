using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Interfaces;

public interface IDomainConfigurationService
{
    Task<DomainConfiguration> CreateDomainConfigurationAsync(DomainType domainType, string name, string? description = null, int defaultDurationMinutes = 60);
    Task<DomainConfiguration?> GetDomainConfigurationByIdAsync(Guid configurationId);
    Task<IEnumerable<DomainConfiguration>> GetAllDomainConfigurationsAsync(bool onlyActive = true);
    Task<DomainConfiguration> UpdateDomainConfigurationAsync(Guid configurationId, string? name = null, string? description = null, int? defaultDurationMinutes = null);
    Task<bool> ActivateDomainConfigurationAsync(Guid configurationId);
    Task<bool> DeactivateDomainConfigurationAsync(Guid configurationId);
    Task<bool> DeleteDomainConfigurationAsync(Guid configurationId);
}