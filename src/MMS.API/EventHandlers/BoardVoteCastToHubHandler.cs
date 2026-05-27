using MediatR;
using Microsoft.AspNetCore.SignalR;
using MMS.API.Hubs;
using MMS.Domain.Events.BoardVoting;

namespace MMS.API.EventHandlers;

public class BoardVoteCastToHubHandler : INotificationHandler<BoardVoteCastEvent>
{
    private readonly IHubContext<BoardVotingHub> _hubContext;
    private readonly ILogger<BoardVoteCastToHubHandler> _logger;

    public BoardVoteCastToHubHandler(
        IHubContext<BoardVotingHub> hubContext,
        ILogger<BoardVoteCastToHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(BoardVoteCastEvent evt, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                submissionId = evt.SubmissionId,
                approveCount = evt.ApproveCount,
                rejectCount = evt.RejectCount,
                totalVotes = evt.TotalVotes
            };

            await _hubContext.Clients
                .Group($"submission:{evt.SubmissionId}")
                .SendAsync("voteCountUpdated", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast voteCountUpdated for submission {SubmissionId}", evt.SubmissionId);
        }
    }
}
