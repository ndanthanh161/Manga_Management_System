using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;
using MMS.Application.DTOs.Series;
using MMS.Domain.Enums;
using System.Security.Claims;
using MMS.Application.Common;
using R = MMS.Application.Common;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class SeriesMutations
{
    [Authorize(Roles = new[] { "Editorial Board" })]
    public async Task<SeriesRankingResponse> AddSeriesRanking(
        [Service] ISeriesService seriesService,
        Guid seriesId,
        int position,
        string period)
    {
        return await seriesService.AddRankingAsync(seriesId, position, period);
    }

    [Authorize(Roles = new[] { "Editorial Board" })]
    public async Task<bool> TriggerSurvivalCheck(
        [Service] ISeriesService seriesService)
    {
        await seriesService.CheckSurvivalAlertsAsync();
        return true;
    }

    [Authorize(Roles = new[] { "Editorial Board" })]
    public async Task<R.Result<SeriesResponse>> VoteSeries(
        [Service] ISeriesService seriesService,
        Guid seriesId,
        VoteType vote,
        Guid? proposedTantouId,
        string? comment,
        ClaimsPrincipal claimsPrincipal)
    {
        try 
        {
            var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
            return await seriesService.VoteSeriesAsync(seriesId, userId, vote, proposedTantouId, comment);
        }
        catch (Exception ex)
        {
            return R.Result<SeriesResponse>.Failure("INTERNAL_ERROR", ex.ToString());
        }
    }
}
