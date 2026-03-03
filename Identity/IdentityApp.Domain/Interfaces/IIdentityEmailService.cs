namespace IdentityApp.Domain.Interfaces;

/// <summary>
/// Service interface for sending emails in the Identity service
/// Handles transactional emails like confirmations, password resets, etc.
/// </summary>
public interface IIdentityEmailService
{
    /// <summary>
    /// Sends an email to a recipient
    /// Supports both HTML and plain text formats
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject line</param>
    /// <param name="body">Email body content</param>
    /// <param name="isHtml">Whether the body is HTML format (defaults to true)</param>
    /// <returns>True if email sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}