using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface ISubmissionTransitionRepository : IGenericRepository<SubmissionTransition>
{
    Task<IEnumerable<SubmissionTransition>> GetBySubmissionIdAsync(Guid submissionId);
}
