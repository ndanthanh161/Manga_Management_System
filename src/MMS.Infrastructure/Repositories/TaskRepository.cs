using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class TaskRepository : GenericRepository<WorkTask>, ITaskRepository
{
    public TaskRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<WorkTask>> GetByAssignedUserIdAsync(Guid userId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Region).ThenInclude(r => r != null ? r.Page : null)
            .Include(t => t.AssignedUser)
            .Where(t => t.AssignedUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<WorkTask>> GetByRegionIdAsync(Guid regionId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.AssignedUser)
            .Where(t => t.RegionId == regionId)
            .ToListAsync();
    }

    public async Task<WorkTask?> GetWithRegionAsync(Guid taskId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Region).ThenInclude(r => r != null ? r.Page : null)
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);
    }

    public async Task<IEnumerable<WorkTask>> GetByPageIdAsync(Guid pageId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Region).ThenInclude(r => r != null ? r.Page : null)
            .Include(t => t.AssignedUser)
            .Where(t => t.Region != null && t.Region.PageId == pageId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<WorkTask>> GetByMangakaIdAsync(Guid mangakaId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Region).ThenInclude(r => r != null ? r.Page : null)
            .Include(t => t.AssignedUser)
            .Where(t => t.AssignedUser != null && t.AssignedUser.MangakaId == mangakaId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}
