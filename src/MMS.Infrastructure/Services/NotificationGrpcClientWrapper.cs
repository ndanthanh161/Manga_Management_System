using Grpc.Core;
using Microsoft.Extensions.Logging;
using MMS.Application.Interfaces.Services;
using MMS.Protos.Notification.V1;

namespace MMS.Infrastructure.Services;

public class NotificationGrpcClientWrapper : INotificationGrpcClient
{
    private readonly MMS.Protos.Notification.V1.NotificationService.NotificationServiceClient _client;
    private readonly ILogger<NotificationGrpcClientWrapper> _logger;

    public NotificationGrpcClientWrapper(
        MMS.Protos.Notification.V1.NotificationService.NotificationServiceClient client,
        ILogger<NotificationGrpcClientWrapper> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendEmailAsync(string userId, string title, string body)
    {
        try
        {
            var response = await _client.SendOneAsync(new SendNotificationRequest
            {
                RecipientUserId = userId,
                Title = title,
                Body = body,
                Channel = "email"
            });

            _logger.LogInformation("gRPC SendOne to UserId '{UserId}' response: success={Success}, id={Id}", 
                userId, response.Success, response.NotificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via gRPC client to UserId '{UserId}'", userId);
        }
    }

    public async Task FanOutEmailAsync(IEnumerable<string> userIds, string title, string body)
    {
        try
        {
            var request = new FanOutRequest
            {
                Title = title,
                Body = body,
                Channel = "email"
            };
            request.RecipientUserIds.AddRange(userIds);

            using var stream = _client.FanOut(request);
            
            _logger.LogInformation("Started gRPC FanOut stream for {Count} users", request.RecipientUserIds.Count);

            await foreach (var status in stream.ResponseStream.ReadAllAsync())
            {
                _logger.LogInformation("gRPC FanOut delivery: userId={UserId}, notificationId={NotificationId}, delivered={Delivered}",
                    status.RecipientUserId, status.NotificationId, status.Delivered);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete FanOut email stream via gRPC client");
        }
    }
}
