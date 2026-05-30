using MMS.Application.DTOs.CandidateApplication;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Enums;
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;
using R = MMS.Application.Common;

using MMS.API.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class CandidateApplicationMutations
{
    [Authorize(Policy = PolicyNames.CanVoteInSession)]
    public async Task<R.Result<CandidateApplicationDto>> VoteApplication(
        [Service] ICandidateApplicationService service,
        Guid applicationId,
        VoteType vote,
        Guid? proposedTantouId,
        string? comment,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await service.VoteApplicationAsync(applicationId, userId, vote, proposedTantouId, comment);
    }
}
