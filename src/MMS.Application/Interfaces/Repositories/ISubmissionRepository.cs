using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Interfaces.Repositories;

public interface ISubmissionRepository : IGenericRepository<Submission>
{
    Task<IEnumerable<Submission>> GetByMangakaIdAsync(Guid mangakaId);
    Task<Submission?> GetWithDetailsAsync(Guid submissionId);
    Task<Submission?> GetTrackedWithDetailsAsync(Guid submissionId);
    Task<IEnumerable<Submission>> GetInboxAsync(Guid userId, string role, WorkflowStatus? filter);
}
