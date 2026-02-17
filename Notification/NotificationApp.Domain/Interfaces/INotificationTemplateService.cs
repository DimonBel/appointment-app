using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service for managing notification templates (Module 2.4)
/// </summary>
public interface INotificationTemplateService
{
    Task<NotificationTemplate> CreateAsync(NotificationTemplate template);
    Task<NotificationTemplate?> GetByIdAsync(Guid id);
    Task<NotificationTemplate?> GetByKeyAsync(string key);
    Task<IEnumerable<NotificationTemplate>> GetAllAsync();
    Task<IEnumerable<NotificationTemplate>> GetByTypeAsync(NotificationType type);
    Task<NotificationTemplate> UpdateAsync(NotificationTemplate template);
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Render a template with the given data dictionary
    /// </summary>
    Task<(string title, string body)> RenderTemplateAsync(string templateKey, Dictionary<string, string> data);
}
