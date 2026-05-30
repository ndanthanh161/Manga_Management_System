using MMS.Application.DTOs.Series;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class SeriesQueries
{
    /// <summary>Lấy danh sách series đang chờ xét duyệt (Draft)</summary>
    [Authorize(Roles = new[] { "Editorial Board", "Admin" })]
    public async Task<IEnumerable<SeriesResponse>> GetPendingSeries(
        [Service] ISeriesService seriesService)
    {
        return await seriesService.GetPendingSeriesAsync();
    }

    /// <summary>Lấy danh sách series của một Mangaka</summary>
    public async Task<IEnumerable<SeriesResponse>> GetMySeries(
        [Service] ISeriesService seriesService,
        Guid mangakaId)
    {
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
