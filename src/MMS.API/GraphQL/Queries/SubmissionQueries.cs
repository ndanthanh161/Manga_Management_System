using MMS.Application.DTOs.Submission;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Enums;
using System.Security.Claims;
using HotChocolate.Authorization;
using MMS.API.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class SubmissionQueries
{
    /// <summary>Xem danh sách submission của một Mangaka</summary>
    [Authorize(Policy = PolicyNames.IsMangaka)]
    public async Task<IEnumerable<SubmissionResponse>> GetMySubmissions(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        [GraphQLType(typeof(UuidType))] Guid? seriesId = null)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.GetByMangakaAsync(mangakaId, seriesId);
        return result.Succeeded ? result.Data! : Enumerable.Empty<SubmissionResponse>();
    }

    /// <summary>Xem chi tiết một submission kèm bản thảo và bình chọn</summary>
    [Authorize]
    public async Task<SubmissionResponse?> GetSubmissionById(
        [Service] ISubmissionService submissionService,
        Guid id)
    {
        var result = await submissionService.GetByIdAsync(id);
        return result.Succeeded ? result.Data : null;
    }

    /// <summary>Lấy danh sách inbox dựa theo role và status filter của người dùng</summary>
    [Authorize]
    public async Task<IEnumerable<SubmissionResponse>> GetSubmissionInbox(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        WorkflowStatus? filter)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        
        string role = "Mangaka"; // default fallback
        if (claimsPrincipal.IsInRole("Editorial Board"))
        {
            role = "Editorial Board";
        }
        else if (claimsPrincipal.IsInRole("Tantou Editor"))
        {
            role = "Tantou";
        }

        var result = await submissionService.GetInboxAsync(userId, role, filter);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Lấy danh sách inbox thất bại.");
        }
        return result.Data!;
    }

    /// <summary>Lấy lịch sử workflow transitions của một bản nộp</summary>
    [Authorize]
    public async Task<IEnumerable<SubmissionTransitionResponse>> GetSubmissionWorkflowHistory(
        [Service] ISubmissionService submissionService,
        Guid submissionId)
    {
        var result = await submissionService.GetWorkflowHistoryAsync(submissionId);
        if (!result.Succeeded)
        {
            throw new Exception(result.Message ?? "Lấy lịch sử workflow thất bại.");
        }
        return result.Data!;
    }
}
