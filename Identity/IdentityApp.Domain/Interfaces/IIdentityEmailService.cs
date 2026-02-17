namespace IdentityApp.Domain.Interfaces;

public interface IIdentityEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}
