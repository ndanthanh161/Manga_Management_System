namespace MMS.Application.Interfaces.Services;

public interface ISubmissionService
{
    Task<SubmissionResponse> CreateAsync(Guid mangakaId, CreateSubmissionRequest request);
    Task<SubmissionResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<SubmissionResponse>> GetByMangakaAsync(Guid mangakaId);
}
