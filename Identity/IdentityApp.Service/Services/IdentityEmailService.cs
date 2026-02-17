using IdentityApp.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace IdentityApp.Service.Services;

public class IdentityEmailService : IIdentityEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityEmailService> _logger;

    public IdentityEmailService(IConfiguration configuration, ILogger<IdentityEmailService> logger)
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
            var fromName = smtpSettings["FromName"] ?? "Booking-app";
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
                bodyBuilder.HtmlBody = body;
                bodyBuilder.TextBody = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]*>", " ").Trim();
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

            _logger.LogInformation("Confirmation email sent successfully to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {ToEmail}", toEmail);
            return false;
        }
    }
}
