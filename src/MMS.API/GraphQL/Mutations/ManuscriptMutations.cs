using MMS.Application.DTOs.Manuscript;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;
using R = MMS.Application.Common;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class ManuscriptMutations
{
    [Authorize]
    public async Task<R.Result<ManuscriptResponse>> SnapshotChapter(
        [Service] IManuscriptService manuscriptService,
        SnapshotChapterRequest input,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await manuscriptService.SnapshotChapterAsync(input.ChapterId, input.Name, userId);
    }

    [Authorize]
    public async Task<R.Result<ManuscriptResponse>> CreateNewVersion(
        [Service] IManuscriptService manuscriptService,
        CreateNewVersionRequest input,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await manuscriptService.CreateNewVersionAsync(input.CurrentManuscriptId, userId);
    }

    [Authorize]
    public async Task<R.Result<ManuscriptResponse>> RevertToVersion(
        [Service] IManuscriptService manuscriptService,
        Guid versionId,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await manuscriptService.RevertToVersionAsync(versionId, userId);
    }
}
