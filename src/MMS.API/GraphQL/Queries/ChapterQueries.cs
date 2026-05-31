using MMS.Application.DTOs.Chapter;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class ChapterQueries
{
    [Authorize]
    public async Task<IEnumerable<ChapterResponse>> GetChaptersBySeries(
        [Service] IChapterService chapterService,
        Guid seriesId)
    {
        return await chapterService.GetBySeriesAsync(seriesId);
    }

    [Authorize]
    public async Task<ChapterResponse?> GetChapterById(
        [Service] IChapterService chapterService,
        Guid id)
    {
        return await chapterService.GetByIdAsync(id);
    }
}
