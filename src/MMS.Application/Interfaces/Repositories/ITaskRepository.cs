using MMS.Domain.Entities;
using TaskEntity = MMS.Domain.Entities.Task;

namespace MMS.Application.Interfaces.Repositories;

public interface ITaskRepository : IGenericRepository<TaskEntity>
{
    Task<IEnumerable<TaskEntity>> GetByAssignedUserIdAsync(Guid userId);
    Task<IEnumerable<TaskEntity>> GetByRegionIdAsync(Guid regionId);
    Task<TaskEntity?> GetWithRegionAsync(Guid taskId);
}
