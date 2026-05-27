using MMS.Application.Common;
using MMS.Application.DTOs.Series;
using MMS.Domain.Enums;

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
    Task<Result<SeriesResponse>> VoteSeriesAsync(Guid seriesId, Guid boardMemberId, VoteType vote, Guid? proposedTantouId, string? comment);
}
