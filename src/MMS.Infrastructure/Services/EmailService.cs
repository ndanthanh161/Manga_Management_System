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

    public async Task SendUnderperformanceAlertAsync(string email, string authorName, string seriesTitle, int currentRanking)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var port = int.Parse(_configuration["Email:Port"] ?? "587");
        var senderEmail = _configuration["Email:SenderEmail"];
        var senderName = _configuration["Email:SenderName"];
        var password = _configuration["Email:Password"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
        {
            // For development, if not configured, just log it or skip
            Console.WriteLine($"[Email Simulation - WARNING] To: {email}, Author: {authorName}, Series: {seriesTitle}, Rank: {currentRanking}");
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
            Subject = $"CẢNH BÁO HIỆU SUẤT TÁC PHẨM: {seriesTitle}",
            Body = $"Kính gửi tác giả {authorName},\n\nTác phẩm \"{seriesTitle}\" của bạn đã nằm trong nhóm 3 tác phẩm có thứ hạng thấp nhất trong 3 kỳ liên tiếp. Hiện tại tác phẩm đang đứng thứ {currentRanking}.\n\nVui lòng cải thiện chất lượng nội dung hoặc liên hệ với Tantou Editor phụ trách để trao đổi thêm. Nếu tiếp tục không đạt hiệu suất tối thiểu, tác phẩm có nguy cơ bị dừng bản thảo (Axed).\n\nTrân trọng,\nBan biên tập.",
            IsBodyHtml = false
        };
        mailMessage.To.Add(email);

        await client.SendMailAsync(mailMessage);
    }
}
