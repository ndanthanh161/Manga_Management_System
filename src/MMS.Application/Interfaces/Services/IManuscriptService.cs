using MMS.Application.Common;
using MMS.Application.DTOs.Manuscript;

namespace MMS.Application.Interfaces.Services;

public interface IManuscriptService
{
    Task<Result<ManuscriptResponse>> SnapshotChapterAsync(Guid chapterId, string name, Guid userId);
    Task<Result<ManuscriptResponse>> CreateNewVersionAsync(Guid currentManuscriptId, Guid userId);
    Task<Result<List<ManuscriptResponse>>> GetVersionHistoryAsync(Guid manuscriptId);
    Task<Result<ManuscriptResponse>> RevertToVersionAsync(Guid versionId, Guid userId);
    Task<Result<ManuscriptResponse>> GetByIdAsync(Guid id);
}
