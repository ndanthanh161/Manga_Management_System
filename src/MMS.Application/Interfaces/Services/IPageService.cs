using MMS.Application.DTOs.Page;

namespace MMS.Application.Interfaces.Services;

public interface IPageService
{
    Task<PageResponse> CreateAsync(CreatePageRequest request);
    Task<IEnumerable<PageResponse>> GetByChapterAsync(Guid chapterId);
}
