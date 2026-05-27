using Microsoft.AspNetCore.SignalR;
using MMS.Application.Interfaces;

namespace MMS.API.Hubs;

public class BoardVotingHub : BaseAuthHub
{
    private readonly IUnitOfWork _uow;

    public BoardVotingHub(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task JoinSubmission(Guid submissionId)
    {
        // Validate: user là Editorial Board hoặc Tantou Editor
        var user = await _uow.Users.GetUserWithRolesAsync(CurrentUserId);
        if (user == null)
            throw new HubException("User not found");

        bool isAuthorized = user.Roles.Any(r => r.Name == "Editorial Board" || r.Name == "Tantou Editor");
        if (!isAuthorized)
            throw new HubException("Only Editorial Board or Tantou can join voting session");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"submission:{submissionId}");
    }

    public async Task JoinApplication(Guid applicationId)
    {
        var user = await _uow.Users.GetUserWithRolesAsync(CurrentUserId);
        if (user == null)
            throw new HubException("User not found");

        bool isAuthorized = user.Roles.Any(r => r.Name == "Editorial Board");
        if (!isAuthorized)
            throw new HubException("Only Editorial Board can join application voting");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"application:{applicationId}");
    }

    // Server → Client events:
    // "voteCountUpdated"             : { submissionId, approveCount, rejectCount, totalVotes }
    // "applicationVoteCountUpdated"  : { applicationId, approveCount, rejectCount, totalVotes }
    // ⚠️ KHÔNG broadcast ai vote gì — chỉ count
}
