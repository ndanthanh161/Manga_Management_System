using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;
using MMS.Application.DTOs.Series;
using MMS.Domain.Enums;
using System.Security.Claims;
using MMS.Application.Common;
using R = MMS.Application.Common;

using MMS.API.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class SeriesMutations
{
    [Authorize(Policy = PolicyNames.CanVoteInSession)]
    public async Task<SeriesRankingResponse> AddSeriesRanking(
        [Service] ISeriesService seriesService,
        Guid seriesId,
        int position,
        string period)
    {
        return await seriesService.AddRankingAsync(seriesId, position, period);
    }

    [Authorize(Policy = PolicyNames.CanVoteInSession)]
    public async Task<bool> TriggerSurvivalCheck(
        [Service] ISeriesService seriesService)
    {
        await seriesService.CheckSurvivalAlertsAsync();
        return true;
    }

    [Authorize(Policy = PolicyNames.IsBoardMember)]
    public async Task<R.Result<SeriesResponse>> VoteSeries(
        [Service] ISeriesService seriesService,
        Guid seriesId,
        VoteType vote,
        Guid? proposedTantouId,
        string? comment,
        ClaimsPrincipal claimsPrincipal)
    {
        var boardMemberId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await seriesService.VoteSeriesAsync(seriesId, boardMemberId, vote, proposedTantouId, comment);
    }
}
