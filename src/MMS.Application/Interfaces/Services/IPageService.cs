using MMS.Application.Common;
using MMS.Application.DTOs.Page;

namespace MMS.Application.Interfaces.Services;

public interface IPageService
{
    Task<PageResponse> CreateAsync(CreatePageRequest request);
    Task<IEnumerable<PageResponse>> GetByChapterAsync(Guid chapterId);
    Task<Result<PageResponse>> UploadPageVersionAsync(Guid chapterId, int pageNumber, 
        Stream fileStream, string fileName, Guid userId, CancellationToken ct = default);
    Task<Result<List<PageResponse>>> GetPageHistoryAsync(Guid chapterId, int pageNumber);
}
