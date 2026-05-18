using Mapster;
using MMS.Application.DTOs.Chapter;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;

namespace MMS.Application.Services;

public class ChapterService : IChapterService
{
    private readonly IUnitOfWork _uow;

    public ChapterService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChapterResponse> CreateAsync(CreateChapterRequest request)
    {
        var series = await _uow.Series.GetByIdAsync(request.SeriesId)
            ?? throw new KeyNotFoundException($"Series {request.SeriesId} not found.");

        var chapter = request.Adapt<Chapter>();
        
        await _uow.Chapters.AddAsync(chapter);
        await _uow.SaveChangesAsync();

        return chapter.Adapt<ChapterResponse>();
    }

    public async Task<IEnumerable<ChapterResponse>> GetBySeriesAsync(Guid seriesId)
    {
        var list = await _uow.Chapters.FindAsync(c => c.SeriesId == seriesId);
        return list.OrderBy(c => c.ChapterNumber).Adapt<IEnumerable<ChapterResponse>>();
    }
}
