using MMS.Application.DTOs.Submission;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Enums;
using System.Security.Claims;
using HotChocolate.Authorization;

using MMS.API.Authorization;

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
        var result = await submissionService.CreateAsync(mangakaId, input);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Khởi tạo bản thảo thất bại.");
        }
        return result.Data!;
    }

    [Authorize(Policy = PolicyNames.CanVoteInSession)]
    public async Task<SubmissionResponse> VoteSubmission(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid submissionId,
        bool approve,
        string note)
    {
        var editorId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.VoteAsync(submissionId, editorId, approve, note);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Bỏ phiếu thất bại.");
        }
        return result.Data!;
    }

    [Authorize(Policy = PolicyNames.CanReviewSubmission)]
    public async Task<SubmissionResponse> ResolveSubmission(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid submissionId,
        bool approved,
        string feedback)
    {
        var editorId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.ResolveAsync(submissionId, editorId, approved, feedback);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Giải quyết bản thảo thất bại.");
        }
        return result.Data!;
    }

    [Authorize]
    public async Task<SubmissionResponse> SubmitManuscript(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid manuscriptId,
        string? note)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.SubmitForReviewAsync(new SubmitForReviewRequest(manuscriptId, note), userId);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Nộp duyệt bản thảo thất bại.");
        }
        return result.Data!;
    }

    [Authorize(Policy = PolicyNames.CanReviewSubmission)]
    public async Task<SubmissionResponse> AcceptSubmission(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid submissionId)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.AcceptForReviewAsync(submissionId, userId);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Tiếp nhận bản nộp thẩm định thất bại.");
        }
        return result.Data!;
    }

    [Authorize(Policy = PolicyNames.CanReviewSubmission)]
    public async Task<SubmissionResponse> ForwardToBoard(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid submissionId,
        string comment)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.ForwardToBoardAsync(submissionId, userId, comment);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Chuyển tiếp bản nộp lên Hội đồng thất bại.");
        }
        return result.Data!;
    }

    [Authorize(Policy = PolicyNames.CanReviewSubmission)]
    public async Task<SubmissionResponse> ReturnForRevision(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid submissionId,
        string comment)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.ReturnForRevisionAsync(submissionId, userId, comment);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Yêu cầu chỉnh sửa lại bản thảo thất bại.");
        }
        return result.Data!;
    }

    [Authorize(Policy = PolicyNames.CanVoteInSession)]
    public async Task<SubmissionResponse> FinalizeBoardDecision(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        Guid submissionId,
        WorkflowStatus decision,
        string comment)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.FinalizeBoardDecisionAsync(submissionId, decision, comment, userId);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Hội đồng đưa ra quyết định cuối thất bại.");
        }
        return result.Data!;
    }
}
