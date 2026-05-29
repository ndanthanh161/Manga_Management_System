using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MMS.NotificationService.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string body)
    {
        // Fallback simulation nếu thiếu cấu hình SMTP thực tế
        if (string.IsNullOrWhiteSpace(_options.Host) || 
            string.IsNullOrWhiteSpace(_options.Username) || 
            string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogWarning("[EMAIL SIMULATION] Config SMTP rỗng. Đang in email ra console log.");
            _logger.LogInformation("[EMAIL SIMULATION]\nTo: {Email} ({Name})\nSubject: {Subject}\nBody:\n{Body}", 
                toEmail, toName, subject, body);
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_options.Username, _options.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }
}
