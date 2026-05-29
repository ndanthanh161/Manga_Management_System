using Mapster;
using MMS.Application.Common;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Application.DTOs.Submission;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

using MMS.Domain.Events.BoardVoting;
using MMS.Domain.Events.Submissions;

namespace MMS.Application.Services;

public class SubmissionService : ISubmissionService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly INotificationGrpcClient _notificationClient;

    public SubmissionService(
        IUnitOfWork uow, 
        IEmailService emailService, 
        IDomainEventPublisher eventPublisher,
        INotificationGrpcClient notificationClient)
    {
        _uow = uow;
        _emailService = emailService;
        _eventPublisher = eventPublisher;
        _notificationClient = notificationClient;
    }

    public async Task<Result<SubmissionResponse>> CreateAsync(Guid? mangakaId, CreateSubmissionRequest request)
    {
        try
        {
            var series = await _uow.Series.GetByIdAsync(request.SeriesId);
            if (series == null)
            {
                return Result<SubmissionResponse>.NotFound("SERIES_NOT_FOUND", $"Bộ truyện {request.SeriesId} không tồn tại.");
            }

            if (series.AuthorId.HasValue && series.AuthorId.Value != Guid.Empty)
            {
                if (!mangakaId.HasValue || series.AuthorId != mangakaId.Value)
                {
                    return Result<SubmissionResponse>.Forbidden("UNAUTHORIZED", "Bạn không có quyền nộp bản thảo cho bộ truyện của tác giả khác.");
                }
            }

            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Note = request.Note,
                SeriesId = request.SeriesId,
                MangakaId = mangakaId,
                Status = WorkflowStatus.Draft,
                SubmittedAt = DateTime.UtcNow
            };

            foreach (var mReq in request.Manuscripts)
            {
                var manuscript = new Manuscript
                {
                    Id = Guid.NewGuid(),
                    Name = mReq.Name,
                    Status = ManuscriptStatus.Draft,
                    SubmissionId = submission.Id,
                    CreatedAt = DateTime.UtcNow,
                    Assets = mReq.AssetUrls.Select(url => new Asset
                    {
                        Id = Guid.NewGuid(),
                        FileUrl = url,
                        Type = "image"
                    }).ToList()
                };
                submission.Manuscripts.Add(manuscript);
            }

            await _uow.Submissions.AddAsync(submission);

            var transition = new SubmissionTransition
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                FromStatus = WorkflowStatus.Draft,
                ToStatus = WorkflowStatus.Draft,
                ActorUserId = mangakaId ?? Guid.Empty,
                Comment = "Khởi tạo bản thảo nháp thành công",
                OccurredAt = DateTime.UtcNow
            };
            await _uow.SubmissionTransitions.AddAsync(transition);

            await _uow.SaveChangesAsync();

            var created = await _uow.Submissions.GetWithDetailsAsync(submission.Id);
            return Result<SubmissionResponse>.Success(created!.Adapt<SubmissionResponse>());
        }
        catch (Exception ex)
        {
            return Result<SubmissionResponse>.Failure("CREATE_FAILED", ex.Message);
        }
    }

    public async Task<Result<SubmissionResponse>> GetByIdAsync(Guid id)
    {
        var sub = await _uow.Submissions.GetWithDetailsAsync(id);
        if (sub == null)
        {
            return Result<SubmissionResponse>.NotFound("SUBMISSION_NOT_FOUND", "Không tìm thấy bản nộp.");
        }
        return Result<SubmissionResponse>.Success(sub.Adapt<SubmissionResponse>());
    }

    public async Task<Result<IEnumerable<SubmissionResponse>>> GetByMangakaAsync(Guid mangakaId)
    {
        var list = await _uow.Submissions.GetByMangakaIdAsync(mangakaId);
        return Result<IEnumerable<SubmissionResponse>>.Success(list.Adapt<IEnumerable<SubmissionResponse>>());
    }

    public async Task<Result<SubmissionResponse>> VoteAsync(Guid submissionId, Guid editorId, bool approve, string note)
    {
        try
        {
            var submission = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            if (submission == null)
            {
                return Result<SubmissionResponse>.NotFound("SUBMISSION_NOT_FOUND", $"Bản nộp {submissionId} không tồn tại.");
            }

            var editor = await _uow.Users.GetByIdAsync(editorId);
            if (editor == null)
            {
                return Result<SubmissionResponse>.NotFound("USER_NOT_FOUND", $"Không tìm thấy biên tập viên {editorId}.");
            }

            if (submission.Status == WorkflowStatus.ForwardedToBoard)
            {
                submission.Status = WorkflowStatus.InBoardVoting;
                _uow.Submissions.Update(submission);

                var transition = new SubmissionTransition
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    FromStatus = WorkflowStatus.ForwardedToBoard,
                    ToStatus = WorkflowStatus.InBoardVoting,
                    ActorUserId = editorId,
                    Comment = "Bắt đầu bỏ phiếu bầu của Hội đồng Biên tập",
                    OccurredAt = DateTime.UtcNow
                };
                await _uow.SubmissionTransitions.AddAsync(transition);
            }

            var vote = new BoardVote
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                BoardMemberId = editorId,
                VoteType = approve ? VoteType.Approve : VoteType.Reject,
                Comment = note,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.BoardVotes.AddAsync(vote);
            await _uow.SaveChangesAsync();

            var votesList = await _uow.BoardVotes.FindAsync(v => v.SubmissionId == submissionId);
            int approveCount = votesList.Count(v => v.VoteType == VoteType.Approve);
            int rejectCount = votesList.Count(v => v.VoteType == VoteType.Reject);

            await _eventPublisher.PublishAsync(new BoardVoteCastEvent(
                submissionId,
                approveCount,
                rejectCount,
                votesList.Count()
            ));

            var updated = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            return Result<SubmissionResponse>.Success(updated!.Adapt<SubmissionResponse>());
        }
        catch (Exception ex)
        {
            return Result<SubmissionResponse>.Failure("VOTE_FAILED", ex.Message);
        }
    }

    public async Task<Result<SubmissionResponse>> ResolveAsync(Guid submissionId, Guid editorId, bool approved, string feedback)
    {
        var decision = approved ? WorkflowStatus.Approved : WorkflowStatus.Rejected;
        return await FinalizeBoardDecisionAsync(submissionId, decision, feedback, editorId);
    }

    // --- State Machine Workflow Implementation ---

    public async Task<Result<SubmissionResponse>> SubmitForReviewAsync(SubmitForReviewRequest request, Guid userId)
    {
        try
        {
            var manuscript = await _uow.Manuscripts.GetByIdAsync(request.ManuscriptId);
            if (manuscript == null)
            {
                return Result<SubmissionResponse>.NotFound("MANUSCRIPT_NOT_FOUND", $"Bản thảo {request.ManuscriptId} không tồn tại.");
            }

            if (manuscript.Status != ManuscriptStatus.Draft)
            {
                return Result<SubmissionResponse>.Failure("INVALID_STATUS", "Bản thảo đã nộp trước đó hoặc không ở trạng thái Nháp.");
            }

            var chapter = await _uow.Chapters.GetByIdAsync(manuscript.ChapterId);
            if (chapter == null)
            {
                return Result<SubmissionResponse>.NotFound("CHAPTER_NOT_FOUND", "Không tìm thấy chương cho bản thảo này.");
            }

            var series = await _uow.Series.GetByIdAsync(chapter.SeriesId);
            if (series == null)
            {
                return Result<SubmissionResponse>.NotFound("SERIES_NOT_FOUND", "Không tìm thấy bộ truyện.");
            }

            if (series.AuthorId != userId)
            {
                return Result<SubmissionResponse>.Forbidden("FORBIDDEN", "Bạn không có quyền nộp bản thảo cho bộ truyện này.");
            }

            if (series.TantouEditorId == null || series.TantouEditorId == Guid.Empty)
            {
                return Result<SubmissionResponse>.Failure("NO_TANTOU_ASSIGNED", "Bộ truyện chưa được gán Biên tập viên phụ trách (Tantou). Vui lòng liên hệ Admin.");
            }

            // Create submission
            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                Title = $"Nộp duyệt: {series.Title} - {chapter.Title}",
                Note = request.Note ?? string.Empty,
                SeriesId = series.Id,
                MangakaId = userId,
                AssignedTantouId = series.TantouEditorId,
                Status = WorkflowStatus.Submitted,
                SubmittedAt = DateTime.UtcNow
            };

            // Link manuscript
            manuscript.SubmissionId = submission.Id;
            manuscript.Status = ManuscriptStatus.Submitted;

            await _uow.Submissions.AddAsync(submission);
            _uow.Manuscripts.Update(manuscript);

            var transition = new SubmissionTransition
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                FromStatus = WorkflowStatus.Draft,
                ToStatus = WorkflowStatus.Submitted,
                ActorUserId = userId,
                Comment = "Gửi bản thảo để Tantou phụ trách xét duyệt",
                OccurredAt = DateTime.UtcNow
            };
            await _uow.SubmissionTransitions.AddAsync(transition);

            await _uow.SaveChangesAsync();

            if (series.TantouEditorId.HasValue)
            {
                await _eventPublisher.PublishAsync(new SubmissionStatusChangedEvent(
                    submission.Id,
                    submission.SeriesId,
                    WorkflowStatus.Draft,
                    WorkflowStatus.Submitted,
                    userId,
                    series.TantouEditorId.Value,
                    "Mangaka nộp bản thảo"
                ));
            }

            var created = await _uow.Submissions.GetWithDetailsAsync(submission.Id);
            return Result<SubmissionResponse>.Success(created!.Adapt<SubmissionResponse>());
        }
        catch (Exception ex)
        {
            return Result<SubmissionResponse>.Failure("SUBMIT_FAILED", ex.Message);
        }
    }

    public async Task<Result<SubmissionResponse>> AcceptForReviewAsync(Guid submissionId, Guid tantouId)
    {
        try
        {
            var submission = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            if (submission == null)
            {
                return Result<SubmissionResponse>.NotFound("SUBMISSION_NOT_FOUND", "Không tìm thấy bản nộp.");
            }

            if (submission.AssignedTantouId != tantouId)
            {
                return Result<SubmissionResponse>.Forbidden("FORBIDDEN", "Bạn không phải Biên tập viên phụ trách (Tantou) được gán cho bản nộp này.");
            }

            SubmissionStateMachine.ValidateTransition(submission.Status, WorkflowStatus.UnderTantouReview);

            var oldStatus = submission.Status;
            submission.Status = WorkflowStatus.UnderTantouReview;

            // Update linked manuscripts status to InReview
            foreach (var ms in submission.Manuscripts)
            {
                ms.Status = ManuscriptStatus.InReview;
                _uow.Manuscripts.Update(ms);
            }

            var transition = new SubmissionTransition
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                FromStatus = oldStatus,
                ToStatus = WorkflowStatus.UnderTantouReview,
                ActorUserId = tantouId,
                Comment = "Biên tập viên phụ trách (Tantou) bắt đầu quá trình thẩm định bản thảo",
                OccurredAt = DateTime.UtcNow
            };

            _uow.Submissions.Update(submission);
            await _uow.SubmissionTransitions.AddAsync(transition);
            await _uow.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new SubmissionStatusChangedEvent(
                submission.Id,
                submission.SeriesId,
                oldStatus,
                WorkflowStatus.UnderTantouReview,
                tantouId,
                submission.MangakaId ?? Guid.Empty,
                "Tantou đã nhận bản thảo để thẩm định"
            ));

            var updated = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            return Result<SubmissionResponse>.Success(updated!.Adapt<SubmissionResponse>());
        }
        catch (Exception ex)
        {
            return Result<SubmissionResponse>.Failure("ACCEPT_FAILED", ex.Message);
        }
    }

    public async Task<Result<SubmissionResponse>> ForwardToBoardAsync(Guid submissionId, Guid tantouId, string comment)
    {
        try
        {
            var submission = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            if (submission == null)
            {
                return Result<SubmissionResponse>.NotFound("SUBMISSION_NOT_FOUND", "Không tìm thấy bản nộp.");
            }

            if (submission.AssignedTantouId != tantouId)
            {
                return Result<SubmissionResponse>.Forbidden("FORBIDDEN", "Bạn không phải Biên tập viên phụ trách (Tantou) được gán cho bản nộp này.");
            }

            SubmissionStateMachine.ValidateTransition(submission.Status, WorkflowStatus.ForwardedToBoard);

            var oldStatus = submission.Status;
            submission.Status = WorkflowStatus.ForwardedToBoard;

            var transition = new SubmissionTransition
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                FromStatus = oldStatus,
                ToStatus = WorkflowStatus.ForwardedToBoard,
                ActorUserId = tantouId,
                Comment = string.IsNullOrWhiteSpace(comment) ? "Gửi lên Hội đồng Biên tập xem xét" : comment,
                OccurredAt = DateTime.UtcNow
            };

            _uow.Submissions.Update(submission);
            await _uow.SubmissionTransitions.AddAsync(transition);
            await _uow.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new SubmissionStatusChangedEvent(
                submission.Id,
                submission.SeriesId,
                oldStatus,
                WorkflowStatus.ForwardedToBoard,
                tantouId,
                submission.MangakaId ?? Guid.Empty,
                string.IsNullOrWhiteSpace(comment) ? "Gửi lên Hội đồng Biên tập xem xét" : comment
            ));

            var updated = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            return Result<SubmissionResponse>.Success(updated!.Adapt<SubmissionResponse>());
        }
        catch (Exception ex)
        {
            return Result<SubmissionResponse>.Failure("FORWARD_FAILED", ex.Message);
        }
    }

    public async Task<Result<SubmissionResponse>> ReturnForRevisionAsync(Guid submissionId, Guid tantouId, string comment)
    {
        try
        {
            var submission = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            if (submission == null)
            {
                return Result<SubmissionResponse>.NotFound("SUBMISSION_NOT_FOUND", "Không tìm thấy bản nộp.");
            }

            if (submission.AssignedTantouId != tantouId)
            {
                return Result<SubmissionResponse>.Forbidden("FORBIDDEN", "Bạn không phải Biên tập viên phụ trách (Tantou) được gán cho bản nộp này.");
            }

            SubmissionStateMachine.ValidateTransition(submission.Status, WorkflowStatus.ReturnedForRevision);

            var oldStatus = submission.Status;
            submission.Status = WorkflowStatus.ReturnedForRevision;

            // Reset manuscripts to Draft so Mangaka can modify
            foreach (var ms in submission.Manuscripts)
            {
                ms.Status = ManuscriptStatus.Draft;
                _uow.Manuscripts.Update(ms);
            }

            var transition = new SubmissionTransition
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                FromStatus = oldStatus,
                ToStatus = WorkflowStatus.ReturnedForRevision,
                ActorUserId = tantouId,
                Comment = string.IsNullOrWhiteSpace(comment) ? "Yêu cầu chỉnh sửa lại bản thảo" : comment,
                OccurredAt = DateTime.UtcNow
            };

            _uow.Submissions.Update(submission);
            await _uow.SubmissionTransitions.AddAsync(transition);
            await _uow.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new SubmissionStatusChangedEvent(
                submission.Id,
                submission.SeriesId,
                oldStatus,
                WorkflowStatus.ReturnedForRevision,
                tantouId,
                submission.MangakaId ?? Guid.Empty,
                string.IsNullOrWhiteSpace(comment) ? "Yêu cầu chỉnh sửa lại bản thảo" : comment
            ));

            var updated = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            return Result<SubmissionResponse>.Success(updated!.Adapt<SubmissionResponse>());
        }
        catch (Exception ex)
        {
            return Result<SubmissionResponse>.Failure("RETURN_FAILED", ex.Message);
        }
    }

    public async Task<Result<SubmissionResponse>> FinalizeBoardDecisionAsync(Guid submissionId, WorkflowStatus decision, string comment, Guid actorId)
    {
        try
        {
            if (decision != WorkflowStatus.Approved && decision != WorkflowStatus.Rejected && decision != WorkflowStatus.Postponed)
            {
                return Result<SubmissionResponse>.Failure("INVALID_DECISION", "Quyết định chỉ có thể là Approved, Rejected hoặc Postponed.");
            }

            var submission = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            if (submission == null)
            {
                return Result<SubmissionResponse>.NotFound("SUBMISSION_NOT_FOUND", "Không tìm thấy bản nộp.");
            }

            SubmissionStateMachine.ValidateTransition(submission.Status, decision);

            var oldStatus = submission.Status;
            submission.Status = decision;
            submission.ResolvedAt = DateTime.UtcNow;

            if (decision == WorkflowStatus.Approved)
            {
                var series = await _uow.Series.GetByIdAsync(submission.SeriesId);
                if (series != null)
                {
                    series.Status = SeriesStatus.Ongoing;
                    if (submission.AssignedTantouId.HasValue)
                    {
                        series.TantouEditorId = submission.AssignedTantouId.Value;
                    }
                    _uow.Series.Update(series);

                    if (!series.AuthorId.HasValue || series.AuthorId.Value == Guid.Empty)
                    {
                        var inviteEmail = series.AuthorEmail;
                        if (!string.IsNullOrEmpty(inviteEmail))
                        {
                            var invite = await _uow.Invites.CreateInvitationAsync(inviteEmail, "Mangaka", actorId);
                            await _uow.SaveChangesAsync(); // save invite
                            await _emailService.SendInvitationAsync(inviteEmail, invite.Token, "Mangaka");
                        }
                    }
                }

                // Update manuscripts status to Published
                foreach (var ms in submission.Manuscripts)
                {
                    ms.Status = ManuscriptStatus.Published;
                    _uow.Manuscripts.Update(ms);
                }
            }

            var transition = new SubmissionTransition
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                FromStatus = oldStatus,
                ToStatus = decision,
                ActorUserId = actorId,
                Comment = string.IsNullOrWhiteSpace(comment) ? $"Quyết định của Hội đồng: {decision}" : comment,
                OccurredAt = DateTime.UtcNow
            };

            _uow.Submissions.Update(submission);
            await _uow.SubmissionTransitions.AddAsync(transition);
            await _uow.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new SubmissionStatusChangedEvent(
                submission.Id,
                submission.SeriesId,
                oldStatus,
                decision,
                actorId,
                submission.MangakaId ?? Guid.Empty,
                string.IsNullOrWhiteSpace(comment) ? $"Quyết định của Hội đồng: {decision}" : comment
            ));

            // Gọi email qua gRPC client trực tiếp 
            if (decision == WorkflowStatus.Approved || decision == WorkflowStatus.Rejected)
            {
                if (submission.MangakaId.HasValue && submission.MangakaId.Value != Guid.Empty)
                {
                    string msg = decision == WorkflowStatus.Approved
                        ? $"Chúc mừng! Bản thảo '{submission.Title}' đã được duyệt xuất bản."
                        : $"Bản thảo '{submission.Title}' chưa được duyệt. Lý do: {comment}";

                    await _notificationClient.SendEmailAsync(submission.MangakaId.Value.ToString(), "Kết quả duyệt bản thảo", msg);
                }
            }

            var updated = await _uow.Submissions.GetWithDetailsAsync(submissionId);
            return Result<SubmissionResponse>.Success(updated!.Adapt<SubmissionResponse>());
        }
        catch (Exception ex)
        {
            return Result<SubmissionResponse>.Failure("FINALIZE_FAILED", ex.Message);
        }
    }

    public async Task<Result<IEnumerable<SubmissionResponse>>> GetInboxAsync(Guid userId, string role, WorkflowStatus? filter)
    {
        try
        {
            var list = await _uow.Submissions.GetInboxAsync(userId, role, filter);
            return Result<IEnumerable<SubmissionResponse>>.Success(list.Adapt<IEnumerable<SubmissionResponse>>());
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SubmissionResponse>>.Failure("GET_INBOX_FAILED", ex.Message);
        }
    }

    public async Task<Result<IEnumerable<SubmissionTransitionResponse>>> GetWorkflowHistoryAsync(Guid submissionId)
    {
        try
        {
            var list = await _uow.SubmissionTransitions.GetBySubmissionIdAsync(submissionId);
            return Result<IEnumerable<SubmissionTransitionResponse>>.Success(list.Adapt<IEnumerable<SubmissionTransitionResponse>>());
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SubmissionTransitionResponse>>.Failure("GET_HISTORY_FAILED", ex.Message);
        }
    }
}
