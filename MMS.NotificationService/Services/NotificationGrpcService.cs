using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MMS.Protos.Notification.V1;

namespace MMS.NotificationService.Services;

public class NotificationGrpcService : MMS.Protos.Notification.V1.NotificationService.NotificationServiceBase
{
    private readonly IEmailSender _emailSender;
    private readonly IUserEmailLookup _userEmailLookup;
    private readonly ILogger<NotificationGrpcService> _logger;

    public NotificationGrpcService(
        IEmailSender emailSender,
        IUserEmailLookup userEmailLookup,
        ILogger<NotificationGrpcService> logger)
    {
        _emailSender = emailSender;
        _userEmailLookup = userEmailLookup;
        _logger = logger;
    }

    public override async Task<SendNotificationResponse> SendOne(
        SendNotificationRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Received SendOne request for RecipientUserId: '{UserId}', Channel: '{Channel}'", 
            request.RecipientUserId, request.Channel);

        string? email = null;
        string name = "";

        // Kiểm tra xem có email trực tiếp trong metadata không (dành cho GrpcEmailService adapter)
        if (request.Metadata.TryGetValue("direct_email", out var directEmail))
        {
            email = directEmail;
            _logger.LogInformation("Found direct_email in metadata: {Email}", email);
        }
        else
        {
            email = await _userEmailLookup.GetEmailAsync(request.RecipientUserId);
            if (email == null)
            {
                _logger.LogWarning("Email not found for RecipientUserId: {UserId}", request.RecipientUserId);
                return new SendNotificationResponse
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    Success = false
                };
            }
        }

        var success = await _emailSender.SendAsync(email, name, request.Title, request.Body);

        return new SendNotificationResponse
        {
            NotificationId = Guid.NewGuid().ToString(),
            Success = success
        };
    }

    public override async Task FanOut(
        FanOutRequest request,
        IServerStreamWriter<DeliveryStatus> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Received FanOut request for {Count} users, Channel: {Channel}", 
            request.RecipientUserIds.Count, request.Channel);

        foreach (var userId in request.RecipientUserIds)
        {
            // Kiểm tra cancel từ client
            context.CancellationToken.ThrowIfCancellationRequested();

            bool delivered = false;

            if (request.Channel == "email")
            {
                var email = await _userEmailLookup.GetEmailAsync(userId);
                if (email != null)
                {
                    delivered = await _emailSender.SendAsync(email, "", request.Title, request.Body);
                }
                else
                {
                    _logger.LogWarning("Email not found for userId in FanOut: {UserId}", userId);
                }
            }

            await responseStream.WriteAsync(new DeliveryStatus
            {
                RecipientUserId = userId,
                NotificationId = Guid.NewGuid().ToString(),
                Delivered = delivered,
                DeliveredAt = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }
    }
}
