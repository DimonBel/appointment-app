namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service for sending emails via SMTP (Module 2.2 - Email Delivery)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email to a specific address
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Send an email to a user by their ID (resolves email from metadata/preferences)
    /// </summary>
    Task<bool> SendEmailToUserAsync(Guid userId, string subject, string body, bool isHtml = true, string? toEmail = null);
}
