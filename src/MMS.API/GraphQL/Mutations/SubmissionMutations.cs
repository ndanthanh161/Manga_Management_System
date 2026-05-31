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



    [Authorize]
    public async Task<SubmissionResponse> SubmitChapterForReview(
        [Service] ISubmissionService submissionService,
        [Service] IManuscriptService manuscriptService,
        ClaimsPrincipal claimsPrincipal,
        Guid chapterId,
        string? note)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        
        // 1. Tự động Snapshot Chapter thành Manuscript
        var snapshotResult = await manuscriptService.SnapshotChapterAsync(chapterId, "Bản thảo nộp duyệt", userId);
        if (!snapshotResult.Succeeded || snapshotResult.Data == null)
        {
            throw new Exception(snapshotResult.Message ?? "Khởi tạo bản thảo từ Chapter thất bại.");
        }

        // 2. Nộp Manuscript vừa tạo
        var manuscriptId = snapshotResult.Data.Id;
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
        string comment,
        Guid? assignTantouId = null)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.FinalizeBoardDecisionAsync(submissionId, decision, comment, userId, assignTantouId);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Hội đồng đưa ra quyết định cuối thất bại.");
        }
        return result.Data!;
    }
}
