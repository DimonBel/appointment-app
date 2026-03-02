using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AutomationApp.Service;

public class NotificationServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationServiceClient> _logger;

    public NotificationServiceClient(
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendBookingRequestNotificationAsync(
        Guid doctorUserId,
        string patientName,
        string specialization,
        DateTime scheduledDateTime,
        Guid? orderId = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("NotificationService");

            var notificationDto = new
            {
                UserId = doctorUserId,
                Title = "New Appointment Request",
                Message = $"Patient {patientName} would like to book an appointment with you for {specialization} on {scheduledDateTime:MMM dd, yyyy} at {scheduledDateTime:hh:mm tt}. Please review and confirm or decline.",
                Type = 17, // BookingRequest notification type
                ReferenceId = orderId,
                ReferenceType = "Order",
                Metadata = JsonSerializer.Serialize(new
                {
                    PatientName = patientName,
                    Specialization = specialization,
                    ScheduledDateTime = scheduledDateTime.ToString("o")
                })
            };

            var requestJson = JsonSerializer.Serialize(notificationDto);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/notifications", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent booking request notification to doctor {DoctorUserId}", doctorUserId);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send notification to doctor {DoctorUserId}: {Error}", doctorUserId, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to doctor {DoctorUserId}", doctorUserId);
            return false;
        }
    }

    public async Task<bool> SendBookingConfirmationNotificationAsync(
        Guid clientUserId,
        string doctorName,
        DateTime scheduledDateTime,
        Guid orderId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("NotificationService");

            var notificationDto = new
            {
                UserId = clientUserId,
                Title = "Appointment Confirmed",
                Message = $"Your appointment with {doctorName} on {scheduledDateTime:MMM dd, yyyy} at {scheduledDateTime:hh:mm tt} has been confirmed!",
                Type = 16, // BookingConfirmation notification type
                ReferenceId = orderId,
                ReferenceType = "Order",
                Metadata = JsonSerializer.Serialize(new
                {
                    DoctorName = doctorName,
                    ScheduledDateTime = scheduledDateTime.ToString("o")
                })
            };

            var requestJson = JsonSerializer.Serialize(notificationDto);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/notifications", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent booking confirmation notification to client {ClientUserId}", clientUserId);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send notification to client {ClientUserId}: {Error}", clientUserId, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to client {ClientUserId}", clientUserId);
            return false;
        }
    }
}