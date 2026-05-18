using Mapster;
using MMS.Application.DTOs.Page;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;

namespace MMS.Application.Services;

public class PageService : IPageService
{
    private readonly IUnitOfWork _uow;

    public PageService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PageResponse> CreateAsync(CreatePageRequest request)
    {
        var chapter = await _uow.Chapters.GetByIdAsync(request.ChapterId)
            ?? throw new KeyNotFoundException($"Chapter {request.ChapterId} not found.");

        var page = request.Adapt<Page>();

        await _uow.Pages.AddAsync(page);
        await _uow.SaveChangesAsync();

        return page.Adapt<PageResponse>();
    }

    public async Task<IEnumerable<PageResponse>> GetByChapterAsync(Guid chapterId)
    {
        var list = await _uow.Pages.FindAsync(p => p.ChapterId == chapterId);
        return list.OrderBy(p => p.PageNumber).Adapt<IEnumerable<PageResponse>>();
    }
}
