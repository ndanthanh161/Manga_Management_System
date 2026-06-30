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
        if (boardMember == null || !boardMember.Roles.Any(r => r.Name == "Editorial Board"))
            return Result<SeriesResponse>.Failure("FORBIDDEN", "Chỉ thành viên Editorial Board mới có thể bỏ phiếu.", 403);

        var series = await _uow.Series.GetByIdAsync(seriesId);
        if (series == null)
            return Result<SeriesResponse>.NotFound("NOT_FOUND", "Series không tồn tại.");

        if (series.Status != SeriesStatus.Draft)
            return Result<SeriesResponse>.Failure("ALREADY_APPROVED", "Series này đã được phê duyệt hoặc không ở trạng thái chờ duyệt.");

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

        if (vote == VoteType.Approve)
        {
            if (proposedTantouId == null) return Result<SeriesResponse>.Failure("EDITOR_REQUIRED", "Vui lòng chọn Tantou Editor.");
            
            series.Status = SeriesStatus.Ongoing;
            series.TantouEditorId = proposedTantouId.Value;
            series.UpdatedAt = DateTime.UtcNow;
            _uow.Series.Update(series);
        }
        else if (vote == VoteType.Reject)
        {
            // Just keep it as Draft so the Mangaka can modify and submit again
            series.UpdatedAt = DateTime.UtcNow;
            _uow.Series.Update(series);
        }

        await _uow.SaveChangesAsync();
        return Result<SeriesResponse>.Success(series.Adapt<SeriesResponse>(), "Đã ghi nhận phiếu bầu và cập nhật trạng thái Series.");
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
