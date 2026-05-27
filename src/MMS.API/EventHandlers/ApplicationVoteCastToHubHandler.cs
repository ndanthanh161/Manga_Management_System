using MediatR;
using Microsoft.AspNetCore.SignalR;
using MMS.API.Hubs;
using MMS.Domain.Events.BoardVoting;

namespace MMS.API.EventHandlers;

public class ApplicationVoteCastToHubHandler : INotificationHandler<ApplicationVoteCastEvent>
{
    private readonly IHubContext<BoardVotingHub> _hubContext;
    private readonly ILogger<ApplicationVoteCastToHubHandler> _logger;

    public ApplicationVoteCastToHubHandler(
        IHubContext<BoardVotingHub> hubContext,
        ILogger<ApplicationVoteCastToHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(ApplicationVoteCastEvent evt, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                applicationId = evt.ApplicationId,
                approveCount = evt.ApproveCount,
                rejectCount = evt.RejectCount,
                totalVotes = evt.TotalVotes
            };

            await _hubContext.Clients
                .Group($"application:{evt.ApplicationId}")
                .SendAsync("applicationVoteCountUpdated", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast applicationVoteCountUpdated for application {ApplicationId}", evt.ApplicationId);
        }
    }
}
