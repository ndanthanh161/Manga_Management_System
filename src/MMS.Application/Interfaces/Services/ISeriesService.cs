namespace MMS.Application.Interfaces.Services;

public interface ISeriesService
{
    Task<SeriesResponse> CreateAsync(Guid? mangakaId, CreateSeriesRequest request);
    Task<SeriesResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<SeriesResponse>> GetByMangakaAsync(Guid mangakaId);
    Task<SeriesResponse> UpdateAsync(Guid id, Guid mangakaId, UpdateSeriesRequest request);
    Task<IEnumerable<SeriesRankingResponse>> GetRankingsAsync(Guid seriesId);
    Task<SeriesRankingResponse> AddRankingAsync(Guid seriesId, int position, string period);
    Task CheckSurvivalAlertsAsync();
    Task<bool> CanUserManageSeriesAsync(Guid seriesId, Guid userId);
}
