using MMS.Application.DTOs.Page;
using MMS.Application.Interfaces.Services;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class PageQueries
{
    public async Task<PageResponse?> PageById(
        [Service] IPageService pageService,
        Guid id)
    {
        return await pageService.GetByIdAsync(id);
    }

    public async Task<IEnumerable<PageResponse>> GetPagesByChapter(
        [Service] IPageService pageService,
        Guid chapterId)
    {
        return await pageService.GetByChapterAsync(chapterId);
    }
}
