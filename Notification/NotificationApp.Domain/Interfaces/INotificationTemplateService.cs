using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing notification templates
/// Handles reusable templates for different notification types with variable substitution
/// </summary>
public interface INotificationTemplateService
{
    /// <summary>
    /// Creates a new notification template
    /// </summary>
    /// <param name="template">Template to create</param>
    /// <returns>Created template</returns>
    Task<NotificationTemplate> CreateAsync(NotificationTemplate template);

    /// <summary>
    /// Retrieves a template by its ID
    /// </summary>
    /// <param name="id">ID of the template</param>
    /// <returns>Template if found, null otherwise</returns>
    Task<NotificationTemplate?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a template by its unique key
    /// </summary>
    /// <param name="key">Template key</param>
    /// <returns>Template if found, null otherwise</returns>
    Task<NotificationTemplate?> GetByKeyAsync(string key);

    /// <summary>
    /// Retrieves all templates
    /// </summary>
    /// <returns>Collection of all templates</returns>
    Task<IEnumerable<NotificationTemplate>> GetAllAsync();

    /// <summary>
    /// Retrieves templates filtered by notification type
    /// </summary>
    /// <param name="type">Notification type to filter by</param>
    /// <returns>Collection of matching templates</returns>
    Task<IEnumerable<NotificationTemplate>> GetByTypeAsync(NotificationType type);

    /// <summary>
    /// Updates an existing template
    /// </summary>
    /// <param name="template">Template to update</param>
    /// <returns>Updated template</returns>
    Task<NotificationTemplate> UpdateAsync(NotificationTemplate template);

    /// <summary>
    /// Deletes a template
    /// </summary>
    /// <param name="id">ID of the template to delete</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Renders a template with the provided data dictionary
    /// Replaces template variables with actual data values
    /// </summary>
    /// <param name="templateKey">Key of the template to render</param>
    /// <param name="data">Dictionary of variable names and values to substitute</param>
    /// <returns>Tuple with rendered title and body</returns>
    Task<(string title, string body)> RenderTemplateAsync(string templateKey, Dictionary<string, string> data);
}