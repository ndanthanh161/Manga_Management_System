using MMS.Application.Common;
using MMS.Application.DTOs.Page;
using System.IO;

namespace MMS.Application.Interfaces.Services;

public interface IPageService
{
    Task<PageResponse> CreateWithImageAsync(Stream imageStream, Guid chapterId, int pageNumber, CancellationToken ct = default);
    Task<IEnumerable<PageResponse>> GetByChapterAsync(Guid chapterId);
    Task<Result<PageResponse>> UploadPageAsync(UploadPageRequest request, CancellationToken ct = default);
}
