using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Infrastructure.Data;
using TaskEntity = MMS.Domain.Entities.Task;

namespace MMS.Infrastructure.Repositories;

public class TaskRepository : GenericRepository<TaskEntity>, ITaskRepository
{
    public TaskRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<TaskEntity>> GetByAssignedUserIdAsync(Guid userId)
    {
        return await _context.Tasks
            .Include(t => t.Region)
            .Where(t => t.AssignedUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskEntity>> GetByRegionIdAsync(Guid regionId)
    {
        return await _context.Tasks
            .Include(t => t.AssignedUser)
            .Where(t => t.RegionId == regionId)
            .ToListAsync();
    }

    public async Task<TaskEntity?> GetWithRegionAsync(Guid taskId)
    {
        return await _context.Tasks
            .Include(t => t.Region).ThenInclude(r => r != null ? r.Page : null)
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);
    }
}
