using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Domain.Enums;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class SubmissionRepository : GenericRepository<Submission>, ISubmissionRepository
{
    public SubmissionRepository(MmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Submission>> GetByMangakaIdAsync(Guid mangakaId)
    {
        return await _context.Submissions
            .AsNoTracking()
            .Include(s => s.Series)
            .Include(s => s.Manuscripts)
            .Include(s => s.Transitions)
            .Where(s => s.MangakaId == mangakaId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<Submission?> GetWithDetailsAsync(Guid submissionId)
    {
        return await _context.Submissions
            .AsNoTrackingWithIdentityResolution()
            .Include(s => s.Series)
            .Include(s => s.Mangaka)
            .Include(s => s.AssignedTantou)
            .Include(s => s.Manuscripts).ThenInclude(m => m.Assets)
            .Include(s => s.Votes).ThenInclude(v => v.BoardMember)
            .Include(s => s.Transitions).ThenInclude(t => t.ActorUser)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
    }

    public async Task<IEnumerable<Submission>> GetInboxAsync(Guid userId, string role, WorkflowStatus? filter)
    {
        var query = _context.Submissions
            .AsNoTracking()
            .Include(s => s.Series)
            .Include(s => s.Mangaka)
            .Include(s => s.AssignedTantou)
            .Include(s => s.Manuscripts)
            .AsQueryable();

        if (role.Equals("Tantou", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.AssignedTantouId == userId);
            if (filter.HasValue)
            {
                query = query.Where(s => s.Status == filter.Value);
            }
            else
            {
                query = query.Where(s => s.Status == WorkflowStatus.Submitted || s.Status == WorkflowStatus.UnderTantouReview);
            }
        }
        else if (role.Equals("Editorial Board", StringComparison.OrdinalIgnoreCase))
        {
            if (filter.HasValue)
            {
                query = query.Where(s => s.Status == filter.Value);
            }
            else
            {
                query = query.Where(s => s.Status == WorkflowStatus.ForwardedToBoard || s.Status == WorkflowStatus.InBoardVoting);
            }
        }
        else if (role.Equals("Mangaka", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.MangakaId == userId);
            if (filter.HasValue)
            {
                query = query.Where(s => s.Status == filter.Value);
            }
        }
        else
        {
            // Admin or other roles see all matching the filter (or all if no filter)
            if (filter.HasValue)
            {
                query = query.Where(s => s.Status == filter.Value);
            }
        }

        return await query.OrderByDescending(s => s.SubmittedAt).ToListAsync();
    }
}
