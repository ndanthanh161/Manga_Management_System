using MMS.Application.Common;
using MMS.Application.DTOs.Series;
using System.IO;

namespace MMS.Application.Interfaces.Services;

public interface ISeriesService
{
    Task<SeriesResponse> CreateWithCoverAsync(Guid? mangakaId, CreateSeriesRequest request, Stream coverStream, CancellationToken ct = default);
    Task<SeriesResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<SeriesResponse>> GetByMangakaAsync(Guid mangakaId);
    Task<SeriesResponse> UpdateWithCoverAsync(Guid id, Guid mangakaId, UpdateSeriesRequest request, Stream coverStream, CancellationToken ct = default);
    Task<IEnumerable<SeriesRankingResponse>> GetRankingsAsync(Guid seriesId);
    Task<SeriesRankingResponse> AddRankingAsync(Guid seriesId, int position, string period);
    Task CheckSurvivalAlertsAsync();
    Task<bool> CanUserManageSeriesAsync(Guid seriesId, Guid userId);

    Task<Result<SeriesResponse>> CreateSeriesWithCoverAsync(Guid? mangakaId, CreateSeriesWithCoverRequest request, CancellationToken ct = default);
    Task<Result<SeriesResponse>> UpdateSeriesWithCoverAsync(Guid seriesId, Guid mangakaId, UpdateSeriesWithCoverRequest request, CancellationToken ct = default);
    Task<Result<SeriesResponse>> ReviewSeriesAsync(ReviewSeriesRequest request, Guid adminUserId);
}
