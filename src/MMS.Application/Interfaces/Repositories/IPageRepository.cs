using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IPageRepository : IGenericRepository<Page>
{
    Task<Page?> GetCurrentByChapterAndNumberAsync(Guid chapterId, int pageNumber);
    Task<List<Page>> GetPageHistoryAsync(Guid chapterId, int pageNumber);
    Task<List<Page>> GetCurrentPagesByChapterAsync(Guid chapterId);
}
