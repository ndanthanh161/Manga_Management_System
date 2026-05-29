using Microsoft.Extensions.Logging;
using MMS.Application.Interfaces.Services;
using MMS.Protos.Notification.V1;

namespace MMS.Infrastructure.Services;

public class GrpcEmailService : IEmailService
{
    private readonly MMS.Protos.Notification.V1.NotificationService.NotificationServiceClient _client;
    private readonly ILogger<GrpcEmailService> _logger;

    public GrpcEmailService(
        MMS.Protos.Notification.V1.NotificationService.NotificationServiceClient client,
        ILogger<GrpcEmailService> _logger)
    {
        this._client = client;
        this._logger = _logger;
    }

    public async Task SendInvitationAsync(string email, string token, string roleName)
    {
        await SendViaGrpcAsync(email,
            "Invitation to Manga Management System",
            $"You have been invited as a {roleName}. Use this token to register: {token}");
    }

    public async Task SendUnderperformanceAlertAsync(string email, string authorName, string seriesTitle, int currentRanking)
    {
        await SendViaGrpcAsync(email,
            $"CẢNH BÁO HIỆU SUẤT TÁC PHẨM: {seriesTitle}",
            $"Kính gửi tác giả {authorName},\n\nTác phẩm \"{seriesTitle}\" đang đứng thứ {currentRanking}. Vui lòng cải thiện chất lượng nội dung.");
    }

    public async Task SendApplicationApprovedAsync(string email, string fullName, string seriesTitle)
    {
        await SendViaGrpcAsync(email,
            "Chúc mừng! Bản thảo của bạn đã được duyệt",
            $"Kính gửi {fullName},\n\nBản thảo \"{seriesTitle}\" đã được duyệt thành công! Hãy đăng nhập để bắt đầu sáng tác.");
    }

    public async Task SendApplicationRejectedAsync(string email, string fullName, string seriesTitle, string? reason)
    {
        var reasonText = string.IsNullOrEmpty(reason) ? "Không đạt yêu cầu." : reason;
        await SendViaGrpcAsync(email,
            "Thông báo kết quả xét duyệt bản thảo",
            $"Kính gửi {fullName},\n\nBản thảo \"{seriesTitle}\" chưa được chấp nhận.\nLý do: {reasonText}");
    }

    private async Task SendViaGrpcAsync(string email, string subject, string body)
    {
        try
        {
            var response = await _client.SendOneAsync(new SendNotificationRequest
            {
                RecipientUserId = "", // Để trống, gRPC server sẽ đọc từ Metadata.direct_email
                Title = subject,
                Body = body,
                Channel = "email",
                Metadata = { ["direct_email"] = email }
            });

            _logger.LogInformation("gRPC email sent to {Email}: success={Success}", email, response.Success);
        }
        catch (Exception ex)
        {
            // gRPC fail -> fallback sang console log
            _logger.LogWarning(ex, "Failed to send email via gRPC to {Email}. Falling back to console log simulation.", email);
            Console.WriteLine($"[Email Simulation - gRPC unavailable]\nTo: {email}\nSubject: {subject}\nBody:\n{body}");
        }
    }
}
