using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class ManuscriptPageRefRepository : GenericRepository<ManuscriptPageRef>, IManuscriptPageRefRepository
{
    public ManuscriptPageRefRepository(MmsDbContext context) : base(context)
    {
    }

    public async Task<List<ManuscriptPageRef>> GetByManuscriptAsync(Guid manuscriptId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(mpr => mpr.Page)
            .Where(mpr => mpr.ManuscriptId == manuscriptId)
            .ToListAsync();
    }
}
