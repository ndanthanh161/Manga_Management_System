using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Domain.Enums;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class AnnotationRepository : GenericRepository<Annotation>, IAnnotationRepository
{
    public AnnotationRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Annotation>> GetByPageIdAsync(Guid pageId)
    {
        return await _context.Annotations
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.ResolvedByUser)
            .Include(a => a.ClosedByUser)
            .Where(a => a.PageId == pageId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, int>> GetUnresolvedCountByChapterAsync(Guid chapterId)
    {
        return await _context.Annotations
            .Where(a => a.Page.ChapterId == chapterId && a.Status != AnnotationStatus.Closed)
            .GroupBy(a => a.PageId)
            .ToDictionaryAsync(x => x.Key, x => x.Count());
    }
}
