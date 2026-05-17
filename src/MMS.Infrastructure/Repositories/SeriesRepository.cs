using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class SeriesRepository : GenericRepository<Series>, ISeriesRepository
{
    public SeriesRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Series>> GetByAuthorIdAsync(Guid authorId)
    {
        return await _context.Series
            .Include(s => s.Author)
            .Where(s => s.AuthorId == authorId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Series?> GetWithRankingsAsync(Guid seriesId)
    {
        return await _context.Series
            .Include(s => s.Author)
            .Include(s => s.Rankings)
            .FirstOrDefaultAsync(s => s.Id == seriesId);
    }
}
