using Mapster;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Services;

public class SubmissionService : ISubmissionService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;

    public SubmissionService(IUnitOfWork uow, IEmailService emailService)
    {
        _uow = uow;
        _emailService = emailService;
    }

    public async Task<SubmissionResponse> CreateAsync(Guid? mangakaId, CreateSubmissionRequest request)
    {
        var series = await _uow.Series.GetByIdAsync(request.SeriesId)
            ?? throw new KeyNotFoundException($"Series {request.SeriesId} not found.");

        if (series.AuthorId.HasValue && series.AuthorId.Value != Guid.Empty)
        {
            if (!mangakaId.HasValue || series.AuthorId != mangakaId.Value)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền nộp bản thảo cho bộ truyện của tác giả khác.");
            }
        }

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Note = request.Note,
            SeriesId = request.SeriesId,
            MangakaId = mangakaId,
            Status = SubmissionStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        // Tạo kèm các bản thảo
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
        await _uow.SaveChangesAsync();

        var created = await _uow.Submissions.GetWithDetailsAsync(submission.Id);
        return created!.Adapt<SubmissionResponse>();
    }

    public async Task<SubmissionResponse?> GetByIdAsync(Guid id)
    {
        var sub = await _uow.Submissions.GetWithDetailsAsync(id);
        return sub?.Adapt<SubmissionResponse>();
    }

    public async Task<IEnumerable<SubmissionResponse>> GetByMangakaAsync(Guid mangakaId)
    {
        var list = await _uow.Submissions.GetByMangakaIdAsync(mangakaId);
        return list.Adapt<IEnumerable<SubmissionResponse>>();
    }

    public async Task<SubmissionResponse> VoteAsync(Guid submissionId, Guid editorId, bool approve, string note)
    {
        var submission = await _uow.Submissions.GetWithDetailsAsync(submissionId)
            ?? throw new KeyNotFoundException($"Submission {submissionId} not found.");

        _ = await _uow.Users.GetByIdAsync(editorId)
            ?? throw new KeyNotFoundException($"Editor {editorId} not found.");

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

        var updated = await _uow.Submissions.GetWithDetailsAsync(submissionId);
        return updated!.Adapt<SubmissionResponse>();
    }

    public async Task<SubmissionResponse> ResolveAsync(Guid submissionId, Guid editorId, bool approved, string feedback)
    {
        var submission = await _uow.Submissions.GetWithDetailsAsync(submissionId)
            ?? throw new KeyNotFoundException($"Submission {submissionId} not found.");

        submission.Status = approved ? SubmissionStatus.Approved : SubmissionStatus.Rejected;

        if (approved)
        {
            // Set Series status to Ongoing and assign TantouEditor
            var series = await _uow.Series.GetWithRankingsAsync(submission.SeriesId)
                ?? throw new KeyNotFoundException($"Series {submission.SeriesId} not found.");

            series.Status = SeriesStatus.Ongoing;
            series.TantouEditorId = editorId;
            _uow.Series.Update(series);

            // Generate user invite if Series has no registered Author yet (anonymous proposal)
            if (!series.AuthorId.HasValue || series.AuthorId.Value == Guid.Empty)
            {
                var inviteEmail = series.AuthorEmail;
                if (!string.IsNullOrEmpty(inviteEmail))
                {
                    var invite = await _uow.Invites.CreateInvitationAsync(inviteEmail, "Mangaka", editorId);
                    await _uow.SaveChangesAsync(); // save invite
                    
                    await _emailService.SendInvitationAsync(inviteEmail, invite.Token, "Mangaka");
                }
            }
        }

        _uow.Submissions.Update(submission);
        await _uow.SaveChangesAsync();

        var updated = await _uow.Submissions.GetWithDetailsAsync(submissionId);
        return updated!.Adapt<SubmissionResponse>();
    }
}
