using MMS.Application.DTOs.Page;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class PageQueries
{
    [Authorize]
    public async Task<PageResponse?> PageById(
        [Service] IPageService pageService,
        Guid id)
    {
        return await pageService.GetByIdAsync(id);
    }

    [Authorize]
    public async Task<IEnumerable<PageResponse>> GetPagesByChapter(
        [Service] IPageService pageService,
        Guid chapterId)
    {
        return await pageService.GetByChapterAsync(chapterId);
    }
}
