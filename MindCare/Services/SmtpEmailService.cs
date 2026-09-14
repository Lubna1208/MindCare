using System.Net;
using System.Net.Mail;

namespace MindCare.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration) => _configuration = configuration;

    public async Task SendAsync(string toEmail, string subject, string htmlMessage)
    {
        var host = _configuration["Email:Smtp:Host"];
        var from = _configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("Email delivery is not configured.");
        }

        using var message = new MailMessage(from, toEmail, subject, htmlMessage) { IsBodyHtml = true };
        using var client = new SmtpClient(host, _configuration.GetValue<int?>("Email:Smtp:Port") ?? 587)
        {
            EnableSsl = _configuration.GetValue("Email:Smtp:UseSsl", true)
        };

        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message);
    }
}
