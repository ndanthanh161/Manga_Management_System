using MMS.Application.Common;
using MMS.Application.DTOs.Series;
using System.IO;

namespace MMS.Application.Interfaces.Services;

public interface ISeriesService
{
    Task<Result<SeriesResponse>> CreateAsync(Guid? mangakaId, CreateSeriesRequest request, CancellationToken ct = default);
    Task<SeriesResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<SeriesResponse>> GetByMangakaAsync(Guid mangakaId);
    Task<Result<SeriesResponse>> UpdateAsync(Guid id, Guid mangakaId, UpdateSeriesRequest request, CancellationToken ct = default);
    Task<IEnumerable<SeriesRankingResponse>> GetRankingsAsync(Guid seriesId);
    Task<SeriesRankingResponse> AddRankingAsync(Guid seriesId, int position, string period);
    Task CheckSurvivalAlertsAsync();
    Task<bool> CanUserManageSeriesAsync(Guid seriesId, Guid userId);

    Task<Result<SeriesResponse>> ReviewSeriesAsync(ReviewSeriesRequest request, Guid adminUserId);
}
