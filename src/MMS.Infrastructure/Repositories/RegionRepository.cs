using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class RegionRepository : GenericRepository<Region>, IRegionRepository
{
    public RegionRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Region>> GetByPageIdAsync(Guid pageId)
    {
        return await _context.Regions
            .Include(r => r.Tasks)
            .Where(r => r.PageId == pageId)
            .ToListAsync();
    }
}
