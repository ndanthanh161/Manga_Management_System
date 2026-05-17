using Mapster;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Services;

public class SeriesService : ISeriesService
{
    private readonly IUnitOfWork _uow;

    public SeriesService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<SeriesResponse> CreateAsync(Guid mangakaId, CreateSeriesRequest request)
    {
        var series = request.Adapt<Series>();
        series.AuthorId = mangakaId; // runtime value từ JWT — mapping không có context này

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
}
