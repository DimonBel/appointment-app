namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service interface for SMTP email delivery
/// Handles sending emails via configured SMTP server
/// Supports HTML and plain text formats
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email to a specific email address
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject line</param>
    /// <param name="body">Email body content</param>
    /// <param name="isHtml">Whether the body is HTML format (defaults to true)</param>
    /// <returns>True if email sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Sends an email to a user by their ID
    /// Resolves email address from user metadata/preferences
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="subject">Email subject line</param>
    /// <param name="body">Email body content</param>
    /// <param name="isHtml">Whether the body is HTML format (defaults to true)</param>
    /// <param name="toEmail">Optional override email address</param>
    /// <returns>True if email sent successfully, false otherwise</returns>
    Task<bool> SendEmailToUserAsync(Guid userId, string subject, string body, bool isHtml = true, string? toEmail = null);
}