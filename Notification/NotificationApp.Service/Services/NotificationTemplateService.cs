using System.Text.RegularExpressions;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Domain.Interfaces;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Service.Services;

public class NotificationTemplateService : INotificationTemplateService
{
    private readonly INotificationTemplateRepository _templateRepository;

    public NotificationTemplateService(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<NotificationTemplate> CreateAsync(NotificationTemplate template)
    {
        return await _templateRepository.CreateAsync(template);
    }

    public async Task<NotificationTemplate?> GetByIdAsync(Guid id)
    {
        return await _templateRepository.GetByIdAsync(id);
    }

    public async Task<NotificationTemplate?> GetByKeyAsync(string key)
    {
        return await _templateRepository.GetByKeyAsync(key);
    }

    public async Task<IEnumerable<NotificationTemplate>> GetAllAsync()
    {
        return await _templateRepository.GetAllAsync();
    }

    public async Task<IEnumerable<NotificationTemplate>> GetByTypeAsync(NotificationType type)
    {
        return await _templateRepository.GetByTypeAsync(type);
    }

    public async Task<NotificationTemplate> UpdateAsync(NotificationTemplate template)
    {
        return await _templateRepository.UpdateAsync(template);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _templateRepository.DeleteAsync(id);
    }

    public async Task<(string title, string body)> RenderTemplateAsync(string templateKey, Dictionary<string, string> data)
    {
        var template = await _templateRepository.GetByKeyAsync(templateKey);
        if (template == null)
        {
            throw new KeyNotFoundException($"Template with key '{templateKey}' not found.");
        }

        var title = RenderPlaceholders(template.TitleTemplate, data);
        var body = RenderPlaceholders(template.BodyTemplate, data);

        return (title, body);
    }

    private static string RenderPlaceholders(string template, Dictionary<string, string> data)
    {
        return Regex.Replace(template, @"\{(\w+)\}", match =>
        {
            var key = match.Groups[1].Value;
            return data.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
