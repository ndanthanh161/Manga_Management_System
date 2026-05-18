using MMS.Application.DTOs.Page;
using MMS.Application.Interfaces.Services;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class PageQueries
{
    public async Task<IEnumerable<PageResponse>> GetPagesByChapter(
        [Service] IPageService pageService,
        Guid chapterId)
    {
        return await pageService.GetByChapterAsync(chapterId);
    }
}
