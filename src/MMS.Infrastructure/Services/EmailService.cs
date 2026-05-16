using Microsoft.Extensions.Configuration;
using MMS.Application.Interfaces.Services;
using System.Net;
using System.Net.Mail;

namespace MMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendInvitationAsync(string email, string token, string roleName)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var port = int.Parse(_configuration["Email:Port"] ?? "587");
        var senderEmail = _configuration["Email:SenderEmail"];
        var senderName = _configuration["Email:SenderName"];
        var password = _configuration["Email:Password"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
        {
            // For development, if not configured, just log it or skip
            Console.WriteLine($"[Email Simulation] To: {email}, Token: {token}, Role: {roleName}");
            return;
        }

        using var client = new SmtpClient(smtpServer, port)
        {
            Credentials = new NetworkCredential(senderEmail, password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = "Invitation to Manga Management System",
            Body = $"You have been invited as a {roleName}. Use this token to register: {token}",
            IsBodyHtml = false
        };
        mailMessage.To.Add(email);

        await client.SendMailAsync(mailMessage);
    }
}
