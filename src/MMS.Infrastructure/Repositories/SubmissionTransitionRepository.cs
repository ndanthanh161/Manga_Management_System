using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class SubmissionTransitionRepository : GenericRepository<SubmissionTransition>, ISubmissionTransitionRepository
{
    public SubmissionTransitionRepository(MmsDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SubmissionTransition>> GetBySubmissionIdAsync(Guid submissionId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.ActorUser)
            .Where(t => t.SubmissionId == submissionId)
            .OrderBy(t => t.OccurredAt)
            .ToListAsync();
    }
}
