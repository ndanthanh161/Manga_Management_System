using Mapster;
using MMS.Application.Common;
using MMS.Application.DTOs.Series;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    public async Task<Result<SeriesResponse>> CreateSeriesWithCoverAsync(Guid? mangakaId, CreateSeriesWithCoverRequest request, CancellationToken ct = default)
    {
        if (request.File == null || request.File.Length == 0)
            return Result<SeriesResponse>.Failure("FILE_EMPTY", "File is empty.");

        var validationResult = await _fileValidator.ValidateImageAsync(request.File.OpenReadStream(), request.File.FileName, 10);
        if (!validationResult.Succeeded)
            return Result<SeriesResponse>.Failure(validationResult.ErrorCode ?? "VALIDATION_FAILED", validationResult.Message ?? "File validation failed.");

        var createRequest = request.Adapt<CreateSeriesRequest>();
        using var stream = request.File.OpenReadStream();
        try
        {
            var series = await CreateWithCoverAsync(mangakaId, createRequest, stream, ct);
            return Result<SeriesResponse>.Success(series);
        }
        catch (Exception ex)
        {
            return Result<SeriesResponse>.Failure("CREATE_ERROR", ex.Message);
        }
    }

    public async Task<Result<SeriesResponse>> UpdateSeriesWithCoverAsync(Guid seriesId, Guid mangakaId, UpdateSeriesWithCoverRequest request, CancellationToken ct = default)
    {
        if (request.File == null || request.File.Length == 0)
            return Result<SeriesResponse>.Failure("FILE_EMPTY", "File is empty.");

        var validationResult = await _fileValidator.ValidateImageAsync(request.File.OpenReadStream(), request.File.FileName, 10);
        if (!validationResult.Succeeded)
            return Result<SeriesResponse>.Failure(validationResult.ErrorCode ?? "VALIDATION_FAILED", validationResult.Message ?? "File validation failed.");

        var updateRequest = request.Adapt<UpdateSeriesRequest>();
        using var stream = request.File.OpenReadStream();
        try
        {
            var series = await UpdateWithCoverAsync(seriesId, mangakaId, updateRequest, stream, ct);
            return Result<SeriesResponse>.Success(series);
        }
        catch (KeyNotFoundException ex)
        {
            return Result<SeriesResponse>.NotFound("SERIES_NOT_FOUND", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<SeriesResponse>.Forbidden("FORBIDDEN", ex.Message);
        }
        catch (Exception ex)
        {
            return Result<SeriesResponse>.Failure("UPDATE_ERROR", ex.Message);
        }
    }

    public async Task<SeriesResponse> CreateWithCoverAsync(
        Guid? mangakaId, CreateSeriesRequest request, Stream coverStream, CancellationToken ct = default)
    {
        // 1. Upload ảnh bìa lên Cloudinary
        var folder = "mms/series_covers";
        var hint = $"cover_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        var uploadResult = await _imageStorage.UploadImageAsync(coverStream, folder, hint, ct);

        if (!uploadResult.Succeeded)
            throw new InvalidOperationException($"Cover upload failed: {uploadResult.Message}");

        // 2. Tạo Series bằng Mapster (Logic mapping đã được đóng gói 100% trong SeriesMapping.cs)
        try
        {
            var series = (request, uploadResult.Data!, mangakaId).Adapt<Series>();

            await _uow.Series.AddAsync(series);
            await _uow.SaveChangesAsync();

            var created = await _uow.Series.GetWithRankingsAsync(series.Id);
            return created!.Adapt<SeriesResponse>();
        }
        catch
        {
            await _imageStorage.DeleteImageAsync(uploadResult.Data!.PublicId, ct);
            throw;
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

    public async Task<SeriesResponse> UpdateWithCoverAsync(
        Guid id, Guid mangakaId, UpdateSeriesRequest request, Stream coverStream, CancellationToken ct = default)
    {
        var series = await _uow.Series.GetWithRankingsAsync(id)
            ?? throw new KeyNotFoundException($"Series {id} not found.");

        if (series.AuthorId != mangakaId)
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa series này.");

        // 1. Upload ảnh bìa mới
        var folder = $"mms/series_{id}";
        var hint = $"cover_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        var uploadResult = await _imageStorage.UploadImageAsync(coverStream, folder, hint, ct);

        if (!uploadResult.Succeeded)
            throw new InvalidOperationException($"Cover upload failed: {uploadResult.Message}");

        // 2. Cập nhật qua Mapster (Logic mapping nằm gọn gàng trong SeriesMapping.cs)
        try
        {
            // A. Cập nhật các thay đổi từ request DTO (bỏ qua null qua config của UpdateSeriesRequest)
            request.Adapt(series);

            // B. Cập nhật ảnh bìa mới và thời gian thông qua config của UploadedImageDto
            uploadResult.Data!.Adapt(series);

            _uow.Series.Update(series);
            await _uow.SaveChangesAsync();

            return series.Adapt<SeriesResponse>();
        }
        catch
        {
            await _imageStorage.DeleteImageAsync(uploadResult.Data!.PublicId, ct);
            throw;
        }
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
