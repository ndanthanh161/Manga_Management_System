using MediatR;
using Microsoft.AspNetCore.SignalR;
using MMS.API.Hubs;
using MMS.Domain.Events.Submissions;

namespace MMS.API.EventHandlers;

public class SubmissionStatusChangedToHubHandler : INotificationHandler<SubmissionStatusChangedEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SubmissionStatusChangedToHubHandler> _logger;

    public SubmissionStatusChangedToHubHandler(
        IHubContext<NotificationHub> hubContext,
        ILogger<SubmissionStatusChangedToHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(SubmissionStatusChangedEvent evt, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                submissionId = evt.SubmissionId,
                newStatus = evt.NewStatus.ToString(),
                message = evt.Message,
                type = "submission_status_changed"
            };

            // Push notification tới user cần được notify
            await _hubContext.Clients
                .Group($"user:{evt.NotifyUserId}")
                .SendAsync("notificationReceived", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast submission status notification to user {UserId}", evt.NotifyUserId);
        }
    }
}
