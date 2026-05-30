using MMS.Application.DTOs.Series;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;
using MMS.API.Authorization;
using System.Security.Claims;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class SeriesQueries
{
    /// <summary>Lấy danh sách series đang chờ xét duyệt (Draft)</summary>
    [Authorize(Policy = PolicyNames.CanVoteInSession)]
    public async Task<IEnumerable<SeriesResponse>> GetPendingSeries(
        [Service] ISeriesService seriesService)
    {
        return await seriesService.GetPendingSeriesAsync();
    }

    /// <summary>Lấy danh sách series của một Mangaka</summary>
    [Authorize(Policy = PolicyNames.IsMangaka)]
    public async Task<IEnumerable<SeriesResponse>> GetMySeries(
        [Service] ISeriesService seriesService,
        ClaimsPrincipal claimsPrincipal)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await seriesService.GetByMangakaAsync(mangakaId);
    }

    /// <summary>Xem chi tiết một series</summary>
    public async Task<SeriesResponse?> GetSeriesById(
        [Service] ISeriesService seriesService,
        Guid id)
    {
        return await seriesService.GetByIdAsync(id);
    }

    /// <summary>Xem lịch sử xếp hạng của một series</summary>
    public async Task<IEnumerable<SeriesRankingResponse>> GetSeriesRankings(
        [Service] ISeriesService seriesService,
        Guid seriesId)
    {
        return await seriesService.GetRankingsAsync(seriesId);
    }
}
