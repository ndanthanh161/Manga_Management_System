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

    public async Task SendApplicationApprovedAsync(string email, string fullName, string seriesTitle)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var port = int.Parse(_configuration["Email:Port"] ?? "587");
        var senderEmail = _configuration["Email:SenderEmail"];
        var senderName = _configuration["Email:SenderName"];
        var password = _configuration["Email:Password"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine($"[Email Simulation - APPROVED] To: {email}, Name: {fullName}, Series: {seriesTitle}");
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
            Subject = "🎉 Chúc mừng! Bản thảo của bạn đã được duyệt - Manga Management System",
            Body = $"Kính gửi {fullName},\n\nBản thảo \"{seriesTitle}\" của bạn đã được duyệt thành công!\n\nTài khoản của bạn đã được kích hoạt với vai trò Mangaka. Hãy đăng nhập bằng Email và Password đã đăng ký để bắt đầu sáng tác.\n\nMột Biên tập viên (Tantou Editor) đã được phân công để hỗ trợ bạn.\n\nTrân trọng,\nBan biên tập Manga Management System.",
            IsBodyHtml = false
        };
        mailMessage.To.Add(email);

        await client.SendMailAsync(mailMessage);
    }

    public async Task SendApplicationRejectedAsync(string email, string fullName, string seriesTitle, string? reason)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var port = int.Parse(_configuration["Email:Port"] ?? "587");
        var senderEmail = _configuration["Email:SenderEmail"];
        var senderName = _configuration["Email:SenderName"];
        var password = _configuration["Email:Password"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine($"[Email Simulation - REJECTED] To: {email}, Name: {fullName}, Series: {seriesTitle}, Reason: {reason ?? "N/A"}");
            return;
        }

        using var client = new SmtpClient(smtpServer, port)
        {
            Credentials = new NetworkCredential(senderEmail, password),
            EnableSsl = true
        };

        var reasonText = string.IsNullOrEmpty(reason) ? "Không đạt yêu cầu." : reason;

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = "Thông báo kết quả xét duyệt bản thảo - Manga Management System",
            Body = $"Kính gửi {fullName},\n\nChúng tôi rất tiếc phải thông báo rằng bản thảo \"{seriesTitle}\" của bạn chưa được chấp nhận.\n\nLý do: {reasonText}\n\nBạn có thể gửi lại bản thảo mới trong tương lai. Chúng tôi luôn chào đón những tài năng mới.\n\nTrân trọng,\nBan biên tập Manga Management System.",
            IsBodyHtml = false
        };
        mailMessage.To.Add(email);

        await client.SendMailAsync(mailMessage);
    }
}
