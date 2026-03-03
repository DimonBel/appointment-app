using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing domain configurations (medical, legal, consulting, etc.)
/// Controls business rules, default settings, and domain-specific parameters for each service type
/// </summary>
public interface IDomainConfigurationService
{
    /// <summary>
    /// Creates a new domain configuration for a specific service type
    /// </summary>
    /// <param name="domainType">Type of domain (Medical, Legal, Consulting, etc.)</param>
    /// <param name="name">Display name of the domain</param>
    /// <param name="description">Optional description of the domain</param>
    /// <param name="defaultDurationMinutes">Default appointment duration in minutes</param>
    /// <returns>Created domain configuration</returns>
    Task<DomainConfiguration> CreateDomainConfigurationAsync(DomainType domainType, string name, string? description = null, int defaultDurationMinutes = 60);

    /// <summary>
    /// Retrieves a domain configuration by its ID
    /// </summary>
    /// <param name="configurationId">ID of the domain configuration</param>
    /// <returns>Domain configuration if found, null otherwise</returns>
    Task<DomainConfiguration?> GetDomainConfigurationByIdAsync(Guid configurationId);

    /// <summary>
    /// Retrieves all domain configurations
    /// </summary>
    /// <param name="onlyActive">If true, only returns active configurations</param>
    /// <returns>Collection of domain configurations</returns>
    Task<IEnumerable<DomainConfiguration>> GetAllDomainConfigurationsAsync(bool onlyActive = true);

    /// <summary>
    /// Updates an existing domain configuration
    /// </summary>
    /// <param name="configurationId">ID of the configuration to update</param>
    /// <param name="name">Optional new display name</param>
    /// <param name="description">Optional new description</param>
    /// <param name="defaultDurationMinutes">Optional new default duration</param>
    /// <returns>Updated domain configuration</returns>
    Task<DomainConfiguration> UpdateDomainConfigurationAsync(Guid configurationId, string? name = null, string? description = null, int? defaultDurationMinutes = null);

    /// <summary>
    /// Activates a deactivated domain configuration
    /// </summary>
    /// <param name="configurationId">ID of the configuration to activate</param>
    /// <returns>True if activated successfully, false otherwise</returns>
    Task<bool> ActivateDomainConfigurationAsync(Guid configurationId);

    /// <summary>
    /// Deactivates an active domain configuration (prevents new bookings)</param>
    /// </summary>
    /// <param name="configurationId">ID of the configuration to deactivate</param>
    /// <returns>True if deactivated successfully, false otherwise</returns>
    Task<bool> DeactivateDomainConfigurationAsync(Guid configurationId);

    /// <summary>
    /// Deletes a domain configuration permanently
    /// </summary>
    /// <param name="configurationId">ID of the configuration to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteDomainConfigurationAsync(Guid configurationId);
}