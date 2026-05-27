using MediatR;
using Microsoft.AspNetCore.SignalR;
using MMS.API.Hubs;
using MMS.Domain.Events.Annotations;

namespace MMS.API.EventHandlers;

public class AnnotationStatusChangedToHubHandler : INotificationHandler<AnnotationStatusChangedEvent>
{
    private readonly IHubContext<AnnotationHub> _hubContext;
    private readonly ILogger<AnnotationStatusChangedToHubHandler> _logger;

    public AnnotationStatusChangedToHubHandler(
        IHubContext<AnnotationHub> hubContext,
        ILogger<AnnotationStatusChangedToHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(AnnotationStatusChangedEvent evt, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                annotationId = evt.AnnotationId,
                oldStatus = evt.OldStatus.ToString(),
                newStatus = evt.NewStatus.ToString(),
                actorId = evt.ActorUserId
            };

            await _hubContext.Clients
                .Group($"chapter:{evt.ChapterId}")
                .SendAsync("annotationStatusChanged", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast annotationStatusChanged for chapter {ChapterId}", evt.ChapterId);
        }
    }
}
