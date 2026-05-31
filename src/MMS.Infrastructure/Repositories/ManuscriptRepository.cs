using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class ManuscriptRepository : GenericRepository<Manuscript>, IManuscriptRepository
{
    public ManuscriptRepository(MmsDbContext context) : base(context)
    {
    }

    public async Task<Manuscript?> GetCurrentByChapterAsync(Guid chapterId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(m => m.PageRefs)
            .FirstOrDefaultAsync(m => m.ChapterId == chapterId && m.IsCurrentVersion);
    }

    public async Task<List<Manuscript>> GetVersionHistoryAsync(Guid manuscriptId)
    {
        var manuscript = await _dbSet.AsNoTracking().FirstOrDefaultAsync(m => m.Id == manuscriptId);
        if (manuscript == null) return new List<Manuscript>();

        return await _dbSet
            .AsNoTracking()
            .Where(m => m.ChapterId == manuscript.ChapterId)
            .OrderBy(m => m.Version)
            .ToListAsync();
    }
}
