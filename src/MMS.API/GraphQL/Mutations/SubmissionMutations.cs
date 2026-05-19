using MMS.Application.DTOs.Submission;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class SubmissionMutations
{
    [Authorize(Roles = new[] { "Mangaka" })]
    public async Task<SubmissionResponse> CreateSubmission(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        CreateSubmissionRequest input)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await submissionService.CreateAsync(mangakaId, input);
    }
}
