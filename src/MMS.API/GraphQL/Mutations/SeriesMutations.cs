using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;
using MMS.Application.DTOs.Series;

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
}
