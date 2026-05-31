using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Domain.Enums;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class SeriesRepository : GenericRepository<Series>, ISeriesRepository
{
    public SeriesRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Series>> GetByAuthorIdAsync(Guid authorId)
    {
        return await _context.Series
            .AsNoTracking()
            .Include(s => s.Author)
            .Where(s => s.AuthorId == authorId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Series?> GetWithRankingsAsync(Guid seriesId)
    {
        return await _context.Series
            .AsNoTracking()
            .Include(s => s.Author)
            .Include(s => s.Rankings)
            .FirstOrDefaultAsync(s => s.Id == seriesId);
    }

    public async Task<IEnumerable<Series>> GetAllWithRankingsAndAuthorAsync()
    {
        return await _context.Series
            .AsNoTracking()
            .Include(s => s.Author)
            .Include(s => s.Rankings)
            .ToListAsync();
    }

    public async Task<IEnumerable<Series>> GetPendingSeriesAsync()
    {
        return await _context.Series
            .AsNoTracking()
            .Include(s => s.Author)
            .Where(s => s.Status == SeriesStatus.Draft)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task AddRankingAsync(Ranking ranking)
    {
        await _context.Rankings.AddAsync(ranking);
    }
}
