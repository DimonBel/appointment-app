using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Domain.Interfaces;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Service.Services;

public class NotificationScheduleService : INotificationScheduleService
{
    private readonly INotificationScheduleRepository _scheduleRepository;
    private readonly INotificationService _notificationService;
    private readonly INotificationTemplateService _templateService;

    public NotificationScheduleService(
        INotificationScheduleRepository scheduleRepository,
        INotificationService notificationService,
        INotificationTemplateService templateService)
    {
        _scheduleRepository = scheduleRepository;
        _notificationService = notificationService;
        _templateService = templateService;
    }

    public async Task<NotificationSchedule> CreateAsync(NotificationSchedule schedule)
    {
        return await _scheduleRepository.CreateAsync(schedule);
    }

    public async Task<NotificationSchedule?> GetByIdAsync(Guid id)
    {
        return await _scheduleRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<NotificationSchedule>> GetPendingSchedulesAsync()
    {
        return await _scheduleRepository.GetPendingAsync(DateTime.UtcNow);
    }

    public async Task<IEnumerable<NotificationSchedule>> GetByUserIdAsync(Guid userId)
    {
        return await _scheduleRepository.GetByUserIdAsync(userId);
    }

    public async Task CancelAsync(Guid id)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id);
        if (schedule != null && !schedule.IsProcessed)
        {
            schedule.IsCancelled = true;
            await _scheduleRepository.UpdateAsync(schedule);
        }
    }

    public async Task MarkAsProcessedAsync(Guid id)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id);
        if (schedule != null)
        {
            schedule.IsProcessed = true;
            schedule.ProcessedAt = DateTime.UtcNow;
            await _scheduleRepository.UpdateAsync(schedule);
        }
    }

    public async Task ScheduleAppointmentReminderAsync(Guid userId, Guid orderId, DateTime appointmentTime, int minutesBefore = 60)
    {
        var schedule = new NotificationSchedule
        {
            UserId = userId,
            ReferenceId = orderId,
            ReferenceType = "Order",
            NotificationType = NotificationType.OrderReminder,
            ScheduledAt = appointmentTime.AddMinutes(-minutesBefore),
            TemplateId = Guid.Parse("11111111-1111-1111-1111-111111111105"), // order_reminder template
            TemplateData = System.Text.Json.JsonSerializer.Serialize(new
            {
                AppointmentDate = appointmentTime.ToString("yyyy-MM-dd"),
                AppointmentTime = appointmentTime.ToString("HH:mm")
            })
        };

        await _scheduleRepository.CreateAsync(schedule);
    }

    public async Task ProcessPendingSchedulesAsync()
    {
        var pendingSchedules = await _scheduleRepository.GetPendingAsync(DateTime.UtcNow);

        foreach (var schedule in pendingSchedules)
        {
            try
            {
                string title = "Scheduled Notification";
                string body = "You have a scheduled notification.";

                // Try to render from template
                if (schedule.TemplateId.HasValue)
                {
                    var templateData = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(schedule.TemplateData))
                    {
                        var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(schedule.TemplateData);
                        if (parsed != null) templateData = parsed;
                    }

                    try
                    {
                        var templateKey = schedule.NotificationType switch
                        {
                            NotificationType.OrderReminder => "order_reminder",
                            NotificationType.OrderCreated => "order_created",
                            _ => null
                        };

                        if (templateKey != null)
                        {
                            var (renderedTitle, renderedBody) = await _templateService.RenderTemplateAsync(templateKey, templateData);
                            title = renderedTitle;
                            body = renderedBody;
                        }
                    }
                    catch { /* Use default title/body */ }
                }

                await _notificationService.SendNotificationAsync(
                    schedule.UserId,
                    schedule.NotificationType,
                    title,
                    body,
                    schedule.ReferenceId,
                    schedule.ReferenceType);

                schedule.IsProcessed = true;
                schedule.ProcessedAt = DateTime.UtcNow;
                await _scheduleRepository.UpdateAsync(schedule);
            }
            catch (Exception)
            {
                // Log error - schedule will be retried next cycle
            }
        }
    }
}
