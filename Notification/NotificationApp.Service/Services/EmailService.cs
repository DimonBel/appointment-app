using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using NotificationApp.Domain.Interfaces;

namespace NotificationApp.Service.Services;

/// <summary>
/// SMTP email sending service using MailKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Smtp");
            var host = smtpSettings["Host"] ?? throw new InvalidOperationException("SMTP Host not configured");
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var username = smtpSettings["Username"] ?? throw new InvalidOperationException("SMTP Username not configured");
            var password = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password not configured");
            var fromEmail = smtpSettings["FromEmail"] ?? username;
            var fromName = smtpSettings["FromName"] ?? "Healthcare Hub";
            var useSsl = bool.Parse(smtpSettings["UseSsl"] ?? "false");
            var useStartTls = bool.Parse(smtpSettings["UseStartTls"] ?? "true");
            var secureSocketRaw = smtpSettings["SecureSocket"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = WrapInHtmlTemplate(subject, body);
                bodyBuilder.TextBody = StripHtml(body);
            }
            else
            {
                bodyBuilder.TextBody = body;
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            SecureSocketOptions socketOptions;
            if (!string.IsNullOrWhiteSpace(secureSocketRaw)
                && Enum.TryParse<SecureSocketOptions>(secureSocketRaw, true, out var parsedOption))
            {
                socketOptions = parsedOption;
            }
            else if (useSsl)
            {
                socketOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (useStartTls)
            {
                socketOptions = SecureSocketOptions.StartTls;
            }
            else
            {
                socketOptions = SecureSocketOptions.Auto;
            }

            await client.ConnectAsync(host, port, socketOptions);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {ToEmail}: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}: {Subject}", toEmail, subject);
            return false;
        }
    }

    public async Task<bool> SendEmailToUserAsync(Guid userId, string subject, string body, bool isHtml = true, string? toEmail = null)
    {
        if (string.IsNullOrEmpty(toEmail))
        {
            _logger.LogWarning("Cannot send email to user {UserId}: no email address provided", userId);
            return false;
        }

        return await SendEmailAsync(toEmail, subject, body, isHtml);
    }

    private static string WrapInHtmlTemplate(string title, string body)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f4f7fa; }}
        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }}
        .header {{ background: linear-gradient(135deg, #2563eb, #1d4ed8); padding: 32px; text-align: center; }}
        .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; font-weight: 600; }}
        .header p {{ color: rgba(255,255,255,0.85); margin: 8px 0 0; font-size: 14px; }}
        .body {{ padding: 32px; color: #374151; line-height: 1.6; font-size: 15px; }}
        .footer {{ padding: 24px 32px; background: #f9fafb; text-align: center; color: #9ca3af; font-size: 12px; border-top: 1px solid #e5e7eb; }}
        .btn {{ display: inline-block; padding: 12px 28px; background: #2563eb; color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: 500; margin: 16px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Healthcare Hub</h1>
            <p>Your Health, Our Priority</p>
        </div>
        <div class=""body"">
            {body}
        </div>
        <div class=""footer"">
            <p>This is an automated message from Healthcare Hub. Please do not reply to this email.</p>
            <p>&copy; {DateTime.UtcNow.Year} Healthcare Hub. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ").Trim();
    }
}
