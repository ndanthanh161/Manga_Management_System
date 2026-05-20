using MMS.Application.DTOs.Submission;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class SubmissionMutations
{
    public async Task<SubmissionResponse> CreateSubmission(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        CreateSubmissionRequest input)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserIdOptional(claimsPrincipal);
        return await submissionService.CreateAsync(mangakaId, input);
    }

    [Authorize(Roles = new[] { "Admin", "Editor", "EditorialBoard" })]
    public async Task<SubmissionResponse> VoteSubmission(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid submissionId,
        bool approve,
        string note)
    {
        var editorId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await submissionService.VoteAsync(submissionId, editorId, approve, note);
    }

    [Authorize(Roles = new[] { "Admin", "Editor", "EditorialBoard" })]
    public async Task<SubmissionResponse> ResolveSubmission(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid submissionId,
        bool approved,
        string feedback)
    {
        var editorId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await submissionService.ResolveAsync(submissionId, editorId, approved, feedback);
    }
}
