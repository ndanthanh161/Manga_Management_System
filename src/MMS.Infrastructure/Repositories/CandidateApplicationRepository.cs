using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Domain.Enums;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class CandidateApplicationRepository : GenericRepository<CandidateApplication>, ICandidateApplicationRepository
{
    public CandidateApplicationRepository(MmsDbContext context) : base(context)
    {
    }

    public async Task<CandidateApplication?> GetWithDetailsAsync(Guid id)
    {
        return await _context.CandidateApplications
            .Include(a => a.User)
            .Include(a => a.Series)
            .Include(a => a.ApprovedByUser)
            .Include(a => a.Assets)
            .Include(a => a.ApplicationVotes)
                .ThenInclude(v => v.BoardMember)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<CandidateApplication>> GetAllPendingAsync()
    {
        return await _context.CandidateApplications
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Series)
            .Include(a => a.Assets)
            .Include(a => a.ApplicationVotes)
                .ThenInclude(v => v.BoardMember)
            .Where(a => a.Status == CandidateApplicationStatus.Pending)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
