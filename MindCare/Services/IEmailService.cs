namespace MindCare.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlMessage);
}
