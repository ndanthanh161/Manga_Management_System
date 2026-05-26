using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;
using MMS.Application.DTOs.Series;
using System.Security.Claims;
using MMS.Application.Common;
using System;
using System.Threading.Tasks;
using R = MMS.Application.Common;
namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class SeriesMutations
{
    [Authorize(Roles = new[] { "Admin", "Editor" })]
    public async Task<SeriesRankingResponse> AddSeriesRanking(
        [Service] ISeriesService seriesService,
        Guid seriesId,
        int position,
        string period)
    {
        return await seriesService.AddRankingAsync(seriesId, position, period);
    }

    [Authorize(Roles = new[] { "Admin", "Editor" })]
    public async Task<bool> TriggerSurvivalCheck(
        [Service] ISeriesService seriesService)
    {
        await seriesService.CheckSurvivalAlertsAsync();
        return true;
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<R.Result<SeriesResponse>> ReviewSeries(
        [Service] ISeriesService seriesService,
        ReviewSeriesRequest request,
        ClaimsPrincipal claimsPrincipal)
    {
        var userIdString = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            return R.Result<SeriesResponse>.Failure("UNAUTHORIZED", "Unauthorized", 401);

        return await seriesService.ReviewSeriesAsync(request, Guid.Parse(userIdString));
    }
}
