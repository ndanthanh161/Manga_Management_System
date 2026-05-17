using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface ISubmissionRepository : IGenericRepository<Submission>
{
    Task<IEnumerable<Submission>> GetByMangakaIdAsync(Guid mangakaId);
    Task<Submission?> GetWithDetailsAsync(Guid submissionId);
}
