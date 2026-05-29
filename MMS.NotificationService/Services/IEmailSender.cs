namespace MMS.NotificationService.Services;

public interface IEmailSender
{
    Task<bool> SendAsync(string toEmail, string toName, string subject, string body);
}
