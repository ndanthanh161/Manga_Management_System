using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface ICandidateApplicationRepository : IGenericRepository<CandidateApplication>
{
    Task<CandidateApplication?> GetWithDetailsAsync(Guid id);
    Task<IEnumerable<CandidateApplication>> GetAllPendingAsync();
}
