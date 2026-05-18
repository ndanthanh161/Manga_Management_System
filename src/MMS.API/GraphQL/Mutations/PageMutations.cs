using MMS.Application.DTOs.Page;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class PageMutations
{
    [Authorize]
    public async Task<PageResponse> CreatePage(
        [Service] IPageService pageService,
        CreatePageRequest input)
    {
        return await pageService.CreateAsync(input);
    }
}
