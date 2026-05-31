using MMS.Application.DTOs.Chapter;

namespace MMS.Application.Interfaces.Services;

public interface IChapterService
{
    Task<ChapterResponse> CreateAsync(CreateChapterRequest request);
    Task<IEnumerable<ChapterResponse>> GetBySeriesAsync(Guid seriesId);
    Task<ChapterResponse?> GetByIdAsync(Guid id);
}
