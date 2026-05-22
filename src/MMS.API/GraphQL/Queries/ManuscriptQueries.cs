using MMS.Application.DTOs.Manuscript;
using MMS.Application.DTOs.Page;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;
using R = MMS.Application.Common;

namespace MMS.API.GraphQL;

[ExtendObjectType("Query")]
public class ManuscriptQueries
{
    [Authorize]
    public async Task<R.Result<ManuscriptResponse>> GetManuscriptById(
        [Service] IManuscriptService manuscriptService,
        Guid id)
    {
        return await manuscriptService.GetByIdAsync(id);
    }

    [Authorize]
    public async Task<R.Result<List<ManuscriptResponse>>> GetVersionHistory(
        [Service] IManuscriptService manuscriptService,
        Guid manuscriptId)
    {
        return await manuscriptService.GetVersionHistoryAsync(manuscriptId);
    }

    [Authorize]
    public async Task<R.Result<List<PageResponse>>> GetPageHistory(
        [Service] IPageService pageService,
        Guid chapterId,
        int pageNumber)
    {
        return await pageService.GetPageHistoryAsync(chapterId, pageNumber);
    }
}
