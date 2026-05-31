using MMS.Application.Common;
using MMS.Application.DTOs.Submission;
using MMS.Domain.Enums;

namespace MMS.Application.Interfaces.Services;

public interface ISubmissionService
{
    Task<Result<SubmissionResponse>> CreateAsync(Guid? mangakaId, CreateSubmissionRequest request);
    Task<Result<SubmissionResponse>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<SubmissionResponse>>> GetByMangakaAsync(Guid mangakaId);
    Task<Result<SubmissionResponse>> VoteAsync(Guid submissionId, Guid editorId, bool approve, string note);


    // New Workflow State Machine methods
    Task<Result<SubmissionResponse>> SubmitForReviewAsync(SubmitForReviewRequest request, Guid userId);
    Task<Result<SubmissionResponse>> AcceptForReviewAsync(Guid submissionId, Guid tantouId);
    Task<Result<SubmissionResponse>> ForwardToBoardAsync(Guid submissionId, Guid tantouId, string comment);
    Task<Result<SubmissionResponse>> ReturnForRevisionAsync(Guid submissionId, Guid tantouId, string comment);
    Task<Result<SubmissionResponse>> FinalizeBoardDecisionAsync(Guid submissionId, WorkflowStatus decision, string comment, Guid actorId, Guid? assignTantouId = null);
    Task<Result<IEnumerable<SubmissionResponse>>> GetInboxAsync(Guid userId, string role, WorkflowStatus? filter);
    Task<Result<IEnumerable<SubmissionTransitionResponse>>> GetWorkflowHistoryAsync(Guid submissionId);
}
