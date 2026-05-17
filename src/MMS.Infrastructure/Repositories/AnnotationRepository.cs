using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class AnnotationRepository : GenericRepository<Annotation>, IAnnotationRepository
{
    public AnnotationRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Annotation>> GetByPageIdAsync(Guid pageId)
    {
        return await _context.Annotations
            .Include(a => a.User)
            .Where(a => a.PageId == pageId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }
}
