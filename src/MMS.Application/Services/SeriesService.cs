using Mapster;
using MMS.Application.Common;
using MMS.Application.DTOs.ImageFile;
using MMS.Application.DTOs.Series;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Services;

public class SeriesService : ISeriesService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly IImageStorageService _imageStorage;
    private readonly IFileValidator _fileValidator;

    public SeriesService(IUnitOfWork uow, IEmailService emailService, IImageStorageService imageStorage, IFileValidator fileValidator)
    {
        _uow = uow;
        _emailService = emailService;
        _imageStorage = imageStorage;
        _fileValidator = fileValidator;
    }

    public async Task<Result<SeriesResponse>> CreateAsync(Guid? mangakaId, CreateSeriesRequest request, CancellationToken ct = default)
    {
        string? coverUrl = request.CoverImageUrl;
        UploadedImageDto? uploadData = null;

        if (request.File != null && request.File.Length > 0)
        {
            // Mở stream 1 lần, seek về 0 sau khi validate để tránh double-read
            using var stream = request.File.OpenReadStream();

            var validationResult = await _fileValidator.ValidateImageAsync(stream, request.File.FileName, 10);
            if (!validationResult.Succeeded)
                return Result<SeriesResponse>.Failure(validationResult.ErrorCode ?? "VALIDATION_FAILED", validationResult.Message ?? "File validation failed.");

            stream.Position = 0;
            var folder = "mms/series_covers";
            var hint = $"cover_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var uploadResult = await _imageStorage.UploadImageAsync(stream, folder, hint, ct);

            if (!uploadResult.Succeeded)
                return Result<SeriesResponse>.Failure("UPLOAD_FAILED", $"Cover upload failed: {uploadResult.Message}");

            uploadData = uploadResult.Data;
            coverUrl = uploadData!.SecureUrl;
        }

        try
        {
            var hasMangaka = mangakaId.GetValueOrDefault() != Guid.Empty;
            var series = request.Adapt<Series>();
            series.AuthorId = hasMangaka ? mangakaId : null;
            series.AuthorEmail = hasMangaka ? null : request.AuthorEmail;
            series.AuthorName = hasMangaka ? null : request.AuthorName;
            series.CoverImageUrl = coverUrl;

            await _uow.Series.AddAsync(series);
            await _uow.SaveChangesAsync();

            var created = await _uow.Series.GetWithRankingsAsync(series.Id);
            return Result<SeriesResponse>.Success(created!.Adapt<SeriesResponse>());
        }
        catch (Exception ex)
        {
            if (uploadData != null)
                await _imageStorage.DeleteImageAsync(uploadData.PublicId, ct);
            return Result<SeriesResponse>.Failure("CREATE_ERROR", ex.Message);
        }
    }

    public async Task<Result<SeriesResponse>> UpdateAsync(Guid id, Guid mangakaId, UpdateSeriesRequest request, CancellationToken ct = default)
    {
        var series = await _uow.Series.GetWithRankingsAsync(id);
        if (series == null)
            return Result<SeriesResponse>.NotFound("SERIES_NOT_FOUND", $"Series {id} not found.");

        if (series.AuthorId != mangakaId)
            return Result<SeriesResponse>.Forbidden("FORBIDDEN", "Bạn không có quyền chỉnh sửa series này.");

        UploadedImageDto? uploadData = null;

        if (request.File != null && request.File.Length > 0)
        {
            // Mở stream 1 lần, seek về 0 sau khi validate để tránh double-read
            using var stream = request.File.OpenReadStream();

            var validationResult = await _fileValidator.ValidateImageAsync(stream, request.File.FileName, 10);
            if (!validationResult.Succeeded)
                return Result<SeriesResponse>.Failure(validationResult.ErrorCode ?? "VALIDATION_FAILED", validationResult.Message ?? "File validation failed.");

            stream.Position = 0;
            var folder = $"mms/series_{id}";
            var hint = $"cover_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var uploadResult = await _imageStorage.UploadImageAsync(stream, folder, hint, ct);

            if (!uploadResult.Succeeded)
                return Result<SeriesResponse>.Failure("UPLOAD_FAILED", $"Cover upload failed: {uploadResult.Message}");

            uploadData = uploadResult.Data;
        }

        try
        {
            request.Adapt(series);

            var newCoverUrl = uploadData?.SecureUrl ?? request.CoverImageUrl;
            if (!string.IsNullOrEmpty(newCoverUrl))
            {
                series.CoverImageUrl = newCoverUrl;
                series.UpdatedAt = DateTime.UtcNow;
            }

            _uow.Series.Update(series);
            await _uow.SaveChangesAsync();

            return Result<SeriesResponse>.Success(series.Adapt<SeriesResponse>());
        }
        catch (Exception ex)
        {
            if (uploadData != null)
                await _imageStorage.DeleteImageAsync(uploadData.PublicId, ct);
            return Result<SeriesResponse>.Failure("UPDATE_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, Guid mangakaId, CancellationToken ct = default)
    {
        var series = await _uow.Series.GetByIdAsync(id);
        if (series == null)
            return Result<bool>.NotFound("SERIES_NOT_FOUND", $"Series {id} not found.");

        if (series.AuthorId != mangakaId)
            return Result<bool>.Forbidden("FORBIDDEN", "Bạn không có quyền xóa series này.");

        try
        {
            _uow.Series.Remove(series);
            await _uow.SaveChangesAsync();

            return Result<bool>.Success(true, "Xóa series thành công.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DELETE_ERROR", ex.Message);
        }
    }

    public async Task<SeriesResponse?> GetByIdAsync(Guid id)
    {
        var series = await _uow.Series.GetWithRankingsAsync(id);
        return series?.Adapt<SeriesResponse>();
    }

    public async Task<IEnumerable<SeriesResponse>> GetByMangakaAsync(Guid mangakaId)
    {
        var list = await _uow.Series.GetByAuthorIdAsync(mangakaId);
        return list.Adapt<IEnumerable<SeriesResponse>>();
    }

    public async Task<IEnumerable<SeriesResponse>> GetPendingSeriesAsync()
    {
        var list = await _uow.Series.GetPendingSeriesAsync();
        return list.Adapt<IEnumerable<SeriesResponse>>();
    }

    public async Task<Result<SeriesResponse>> VoteSeriesAsync(Guid seriesId, Guid boardMemberId, VoteType vote, Guid? proposedTantouId, string? comment)
    {
        var boardMember = await _uow.Users.GetUserWithRolesAsync(boardMemberId);
        if (boardMember == null || !boardMember.IsActive || !boardMember.Roles.Any(r => r.Name == "Editorial Board"))
            return Result<SeriesResponse>.Failure("FORBIDDEN", "Chỉ thành viên Editorial Board đang hoạt động mới có thể bỏ phiếu.", 403);

        if (vote != VoteType.Approve && vote != VoteType.Reject)
            return Result<SeriesResponse>.Failure("INVALID_VOTE", "Phiếu bầu chỉ có thể là Approve hoặc Reject.");

        var series = await _uow.Series.GetByIdAsync(seriesId);
        if (series == null)
            return Result<SeriesResponse>.NotFound("NOT_FOUND", "Series không tồn tại.");

        if (series.Status != SeriesStatus.Draft)
            return Result<SeriesResponse>.Failure("ALREADY_APPROVED", "Series này đã được phê duyệt hoặc không ở trạng thái chờ duyệt.");

        var existingVotes = await _uow.SeriesVotes.FindAsync(v =>
            v.SeriesId == seriesId && v.BoardMemberId == boardMemberId);
        if (existingVotes.Any())
            return Result<SeriesResponse>.Failure("ALREADY_VOTED", "Bạn đã bỏ phiếu cho Series này rồi.");

        if (vote == VoteType.Approve)
        {
            if (proposedTantouId == null || proposedTantouId == Guid.Empty)
                return Result<SeriesResponse>.Failure("EDITOR_REQUIRED", "Vui lòng chọn Tantou Editor.");

            var proposedTantou = await _uow.Users.GetUserWithRolesAsync(proposedTantouId.Value);
            if (proposedTantou == null || !proposedTantou.IsActive || !proposedTantou.Roles.Any(r => r.Name == "Tantou Editor"))
            {
                return Result<SeriesResponse>.Failure(
                    "INVALID_EDITOR",
                    "Tantou Editor được đề xuất không tồn tại hoặc không còn hoạt động.");
            }
        }

        var newVote = new SeriesVote
        {
            Id = Guid.NewGuid(),
            SeriesId = seriesId,
            BoardMemberId = boardMemberId,
            VoteType = vote,
            ProposedTantouId = (vote == VoteType.Approve) ? proposedTantouId : null,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.SeriesVotes.AddAsync(newVote);
        await _uow.SaveChangesAsync();

        var activeBoardMembers = (await _uow.Users.FindAsync(u =>
            u.IsActive && u.Roles.Any(r => r.Name == "Editorial Board"))).ToList();
        var activeBoardMemberIds = activeBoardMembers.Select(u => u.Id).ToHashSet();
        var votes = (await _uow.SeriesVotes.FindAsync(v => v.SeriesId == seriesId))
            .Where(v => activeBoardMemberIds.Contains(v.BoardMemberId))
            .GroupBy(v => v.BoardMemberId)
            .Select(g => g.OrderByDescending(v => v.CreatedAt).First())
            .ToList();

        int requiredVotes = activeBoardMembers.Count / 2 + 1;
        int approveCount = votes.Count(v => v.VoteType == VoteType.Approve);
        int rejectCount = votes.Count(v => v.VoteType == VoteType.Reject);

        if (approveCount >= requiredVotes)
        {
            var selectedTantouId = votes
                .Where(v => v.VoteType == VoteType.Approve && v.ProposedTantouId.HasValue)
                .GroupBy(v => v.ProposedTantouId!.Value)
                .Select(g => new { TantouId = g.Key, Count = g.Count(), EarliestVote = g.Min(v => v.CreatedAt) })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.EarliestVote)
                .Select(g => (Guid?)g.TantouId)
                .FirstOrDefault();

            if (!selectedTantouId.HasValue)
                return Result<SeriesResponse>.Failure("EDITOR_REQUIRED", "Không xác định được Tantou Editor từ các phiếu Approve.");

            series.Status = SeriesStatus.Ongoing;
            series.TantouEditorId = selectedTantouId.Value;
            series.UpdatedAt = DateTime.UtcNow;
            _uow.Series.Update(series);
            await _uow.SaveChangesAsync();

            return Result<SeriesResponse>.Success(
                series.Adapt<SeriesResponse>(),
                $"Series đã được duyệt với {approveCount}/{activeBoardMembers.Count} phiếu Approve.");
        }

        if (rejectCount >= requiredVotes)
        {
            return Result<SeriesResponse>.Success(
                series.Adapt<SeriesResponse>(),
                $"Series không được duyệt với {rejectCount}/{activeBoardMembers.Count} phiếu Reject.");
        }

        return Result<SeriesResponse>.Success(
            series.Adapt<SeriesResponse>(),
            $"Đã ghi nhận phiếu bầu. Approve: {approveCount}, Reject: {rejectCount}, cần {requiredVotes}/{activeBoardMembers.Count} phiếu để quyết định.");
    }



    public async Task<IEnumerable<SeriesRankingResponse>> GetRankingsAsync(Guid seriesId)
    {
        var series = await _uow.Series.GetWithRankingsAsync(seriesId)
            ?? throw new KeyNotFoundException($"Series {seriesId} not found.");

        return series.Rankings
            .OrderByDescending(r => r.CalculatedAt)
            .Adapt<IEnumerable<SeriesRankingResponse>>();
    }

    public async Task<SeriesRankingResponse> AddRankingAsync(Guid seriesId, int position, string period)
    {
        var series = await _uow.Series.GetByIdAsync(seriesId)
            ?? throw new KeyNotFoundException($"Series {seriesId} not found.");

        var ranking = new Ranking
        {
            Id = Guid.NewGuid(),
            SeriesId = seriesId,
            Position = position,
            Period = period,
            CalculatedAt = DateTime.UtcNow
        };

        await _uow.Series.AddRankingAsync(ranking);
        await _uow.SaveChangesAsync();

        // Map to response manually to ensure proper mapping including SeriesTitle
        var response = new SeriesRankingResponse(
            SeriesId: seriesId,
            SeriesTitle: series.Title,
            Position: position,
            Period: period,
            CalculatedAt: ranking.CalculatedAt
        );

        return response;
    }


    public async Task CheckSurvivalAlertsAsync()
    {
        var allSeries = (await _uow.Series.GetAllWithRankingsAndAuthorAsync()).ToList();

        // 1. Get distinct rounded calculated times (batches)
        var distinctTimes = allSeries.SelectMany(s => s.Rankings)
            .Select(r => new DateTime(r.CalculatedAt.Year, r.CalculatedAt.Month, r.CalculatedAt.Day, r.CalculatedAt.Hour, r.CalculatedAt.Minute, 0, r.CalculatedAt.Kind))
            .Distinct()
            .OrderByDescending(t => t)
            .ToList();

        if (distinctTimes.Count < 3) return;
        var top3Batches = distinctTimes.Take(3).ToList();

        var warningSeries = new List<Series>();

        foreach (var series in allSeries)
        {
            bool inBottomInAll = true;

            foreach (var batchTime in top3Batches)
            {
                // Find all rankings in this batch
                var batchRankings = allSeries.SelectMany(s => s.Rankings)
                    .Where(r => {
                        var rounded = new DateTime(r.CalculatedAt.Year, r.CalculatedAt.Month, r.CalculatedAt.Day, r.CalculatedAt.Hour, r.CalculatedAt.Minute, 0, r.CalculatedAt.Kind);
                        return rounded == batchTime;
                    })
                    .OrderByDescending(r => r.Position)
                    .Take(3)
                    .Select(r => r.SeriesId)
                    .ToList();

                if (!batchRankings.Contains(series.Id))
                {
                    inBottomInAll = false;
                    break;
                }
            }

            if (inBottomInAll)
            {
                warningSeries.Add(series);
            }
        }

        foreach (var series in warningSeries)
        {
            var latestRanking = series.Rankings.OrderByDescending(r => r.CalculatedAt).FirstOrDefault();
            int currentRanking = latestRanking?.Position ?? 0;

            string? email = series.AuthorEmail ?? series.Author?.Email;
            string name = series.AuthorName ?? series.Author?.Username ?? "Tác giả";

            if (!string.IsNullOrEmpty(email))
            {
                await _emailService.SendUnderperformanceAlertAsync(email, name, series.Title, currentRanking);
            }

            if (series.AuthorId.HasValue && series.AuthorId.Value != Guid.Empty)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = series.AuthorId.Value,
                    Message = $"Cảnh báo hiệu suất: Tác phẩm \"{series.Title}\" của bạn đã nằm trong nhóm 3 tác phẩm thấp nhất trong 3 kỳ liên tiếp (Hạng hiện tại: {currentRanking}). Vui lòng trao đổi với Tantou Editor của bạn.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Notifications.AddAsync(notification);
            }
        }

        if (warningSeries.Any())
        {
            await _uow.SaveChangesAsync();
        }
    }

    public async Task<bool> CanUserManageSeriesAsync(Guid seriesId, Guid userId)
    {
        var series = await _uow.Series.GetByIdAsync(seriesId);
        if (series == null) return false;

        return series.AuthorId == userId || series.TantouEditorId == userId;
    }


}
