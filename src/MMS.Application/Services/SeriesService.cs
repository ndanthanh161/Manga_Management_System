using Mapster;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Services;

public class SeriesService : ISeriesService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;

    public SeriesService(IUnitOfWork uow, IEmailService emailService)
    {
        _uow = uow;
        _emailService = emailService;
    }

    public async Task<SeriesResponse> CreateAsync(Guid? mangakaId, CreateSeriesRequest request)
    {
        var series = request.Adapt<Series>();
        
        if (mangakaId.HasValue && mangakaId.Value != Guid.Empty)
        {
            series.AuthorId = mangakaId;
            series.Status = SeriesStatus.Ongoing;
        }
        else
        {
            series.AuthorId = null;
            series.Status = SeriesStatus.Draft;
            series.AuthorEmail = request.AuthorEmail;
            series.AuthorName = request.AuthorName;
        }

        await _uow.Series.AddAsync(series);
        await _uow.SaveChangesAsync();

        // Reload kèm Author để Adapt đúng AuthorName
        var created = await _uow.Series.GetWithRankingsAsync(series.Id);
        return created!.Adapt<SeriesResponse>();
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

    public async Task<SeriesResponse> UpdateAsync(Guid id, Guid mangakaId, UpdateSeriesRequest request)
    {
        var series = await _uow.Series.GetWithRankingsAsync(id)
            ?? throw new KeyNotFoundException($"Series {id} not found.");

        if (series.AuthorId != mangakaId)
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa series này.");

        request.Adapt(series); // IgnoreNullValues(true) — chỉ ghi đè field có giá trị

        _uow.Series.Update(series);
        await _uow.SaveChangesAsync();

        return series.Adapt<SeriesResponse>();
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
