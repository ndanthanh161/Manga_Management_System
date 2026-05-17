using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class SubmissionRepository : GenericRepository<Submission>, ISubmissionRepository
{
    public SubmissionRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Submission>> GetByMangakaIdAsync(Guid mangakaId)
    {
        return await _context.Submissions
            .Include(s => s.Series)
            .Include(s => s.Manuscripts)
            .Where(s => s.MangakaId == mangakaId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<Submission?> GetWithDetailsAsync(Guid submissionId)
    {
        return await _context.Submissions
            .Include(s => s.Series)
            .Include(s => s.Mangaka)
            .Include(s => s.Manuscripts).ThenInclude(m => m.Assets)
            .Include(s => s.Votes).ThenInclude(v => v.BoardMember)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
    }
}
