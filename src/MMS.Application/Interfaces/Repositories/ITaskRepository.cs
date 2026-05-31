using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface ITaskRepository : IGenericRepository<WorkTask>
{
    Task<IEnumerable<WorkTask>> GetByAssignedUserIdAsync(Guid userId);
    Task<IEnumerable<WorkTask>> GetByRegionIdAsync(Guid regionId);
    Task<WorkTask?> GetWithRegionAsync(Guid taskId);
    Task<IEnumerable<WorkTask>> GetByPageIdAsync(Guid pageId);
    Task<IEnumerable<WorkTask>> GetByMangakaIdAsync(Guid mangakaId);
}
