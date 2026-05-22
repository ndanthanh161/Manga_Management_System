using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IManuscriptRepository : IGenericRepository<Manuscript>
{
    Task<Manuscript?> GetCurrentByChapterAsync(Guid chapterId);
    Task<List<Manuscript>> GetVersionHistoryAsync(Guid manuscriptId);
}
