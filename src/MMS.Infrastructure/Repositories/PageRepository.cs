using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class PageRepository : GenericRepository<Page>, IPageRepository
{
    public PageRepository(MmsDbContext context) : base(context)
    {
    }

    public async Task<Page?> GetCurrentByChapterAndNumberAsync(Guid chapterId, int pageNumber)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ChapterId == chapterId && p.PageNumber == pageNumber && p.IsCurrentVersion);
    }

    public async Task<List<Page>> GetPageHistoryAsync(Guid chapterId, int pageNumber)
    {
        return await _dbSet
            .Where(p => p.ChapterId == chapterId && p.PageNumber == pageNumber)
            .OrderBy(p => p.Version)
            .ToListAsync();
    }

    public async Task<List<Page>> GetCurrentPagesByChapterAsync(Guid chapterId)
    {
        return await _dbSet
            .Where(p => p.ChapterId == chapterId && p.IsCurrentVersion)
            .OrderBy(p => p.PageNumber)
            .ToListAsync();
    }
}
