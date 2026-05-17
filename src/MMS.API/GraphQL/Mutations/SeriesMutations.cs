using MMS.Application.DTOs.Series;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class SeriesMutations
{
    [Authorize]
    public async Task<SeriesResponse> CreateSeries(
        [Service] ISeriesService seriesService,
        ClaimsPrincipal claimsPrincipal,
        CreateSeriesRequest input)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await seriesService.CreateAsync(mangakaId, input);
    }

    [Authorize]
    public async Task<SeriesResponse> UpdateSeries(
        [Service] ISeriesService seriesService,
        ClaimsPrincipal claimsPrincipal,
        Guid seriesId,
        UpdateSeriesRequest input)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await seriesService.UpdateAsync(seriesId, mangakaId, input);
    }
}
