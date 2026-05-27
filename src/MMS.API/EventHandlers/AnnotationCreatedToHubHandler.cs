using MediatR;
using Microsoft.AspNetCore.SignalR;
using MMS.API.Hubs;
using MMS.Domain.Events.Annotations;

namespace MMS.API.EventHandlers;

public class AnnotationCreatedToHubHandler : INotificationHandler<AnnotationCreatedEvent>
{
    private readonly IHubContext<AnnotationHub> _hubContext;
    private readonly ILogger<AnnotationCreatedToHubHandler> _logger;

    public AnnotationCreatedToHubHandler(
        IHubContext<AnnotationHub> hubContext,
        ILogger<AnnotationCreatedToHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(AnnotationCreatedEvent evt, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                annotationId = evt.AnnotationId,
                pageId = evt.PageId,
                category = evt.Category,
                x = evt.X,
                y = evt.Y,
                width = evt.Width,
                height = evt.Height,
                createdBy = evt.CreatedByUserId
            };

            await _hubContext.Clients
                .Group($"chapter:{evt.ChapterId}")
                .SendAsync("annotationAdded", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast annotationAdded to chapter {ChapterId}", evt.ChapterId);
        }
    }
}
