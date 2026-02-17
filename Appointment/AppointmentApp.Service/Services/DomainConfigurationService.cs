using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;

namespace AppointmentApp.Service.Services;

public class DomainConfigurationService : IDomainConfigurationService
{
    private readonly IDomainConfigurationRepository _domainConfigurationRepository;

    public DomainConfigurationService(IDomainConfigurationRepository domainConfigurationRepository)
    {
        _domainConfigurationRepository = domainConfigurationRepository;
    }

    public async Task<DomainConfiguration> CreateDomainConfigurationAsync(DomainType domainType, string name, string? description = null, int defaultDurationMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required", nameof(name));
        }

        var domainConfiguration = new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            DomainType = domainType,
            Name = name,
            Description = description,
            DefaultDurationMinutes = defaultDurationMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return await _domainConfigurationRepository.CreateAsync(domainConfiguration);
    }

    public async Task<DomainConfiguration?> GetDomainConfigurationByIdAsync(Guid configurationId)
    {
        return await _domainConfigurationRepository.GetByIdAsync(configurationId);
    }

    public async Task<IEnumerable<DomainConfiguration>> GetAllDomainConfigurationsAsync(bool onlyActive = true)
    {
        return await _domainConfigurationRepository.GetAllAsync(onlyActive);
    }

    public async Task<DomainConfiguration> UpdateDomainConfigurationAsync(Guid configurationId, string? name = null, string? description = null, int? defaultDurationMinutes = null)
    {
        var configuration = await _domainConfigurationRepository.GetByIdAsync(configurationId);
        if (configuration == null)
        {
            throw new ArgumentException("Domain configuration not found", nameof(configurationId));
        }

        if (name != null) configuration.Name = name;
        if (description != null) configuration.Description = description;
        if (defaultDurationMinutes.HasValue) configuration.DefaultDurationMinutes = defaultDurationMinutes.Value;
        configuration.UpdatedAt = DateTime.UtcNow;

        return await _domainConfigurationRepository.UpdateAsync(configuration);
    }

    public async Task<bool> ActivateDomainConfigurationAsync(Guid configurationId)
    {
        var configuration = await _domainConfigurationRepository.GetByIdAsync(configurationId);
        if (configuration == null)
        {
            throw new ArgumentException("Domain configuration not found", nameof(configurationId));
        }

        configuration.IsActive = true;
        configuration.UpdatedAt = DateTime.UtcNow;

        await _domainConfigurationRepository.UpdateAsync(configuration);
        return true;
    }

    public async Task<bool> DeactivateDomainConfigurationAsync(Guid configurationId)
    {
        var configuration = await _domainConfigurationRepository.GetByIdAsync(configurationId);
        if (configuration == null)
        {
            throw new ArgumentException("Domain configuration not found", nameof(configurationId));
        }

        configuration.IsActive = false;
        configuration.UpdatedAt = DateTime.UtcNow;

        await _domainConfigurationRepository.UpdateAsync(configuration);
        return true;
    }

    public async Task<bool> DeleteDomainConfigurationAsync(Guid configurationId)
    {
        return await _domainConfigurationRepository.DeleteAsync(configurationId);
    }
}