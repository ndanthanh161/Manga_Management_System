using Mapster;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Services;

public class SubmissionService : ISubmissionService
{
    private readonly IUnitOfWork _uow;

    public SubmissionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<SubmissionResponse> CreateAsync(Guid mangakaId, CreateSubmissionRequest request)
    {
        var series = await _uow.Series.GetByIdAsync(request.SeriesId)
            ?? throw new KeyNotFoundException($"Series {request.SeriesId} not found.");

        if (series.AuthorId != mangakaId)
            throw new UnauthorizedAccessException("Bạn không có quyền nộp series này.");

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

        // Tạo kèm các bản thảo (giữ logic riêng vì AssetUrls cần chuyển thành Asset entities)
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
}
