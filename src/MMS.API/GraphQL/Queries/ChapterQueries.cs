using MMS.Application.DTOs.Chapter;
using MMS.Application.Interfaces.Services;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class ChapterQueries
{
    public async Task<IEnumerable<ChapterResponse>> GetChaptersBySeries(
        [Service] IChapterService chapterService,
        Guid seriesId)
    {
        return await chapterService.GetBySeriesAsync(seriesId);
    }
}
