using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface ISeriesRepository : IGenericRepository<Series>
{
    Task<IEnumerable<Series>> GetByAuthorIdAsync(Guid authorId);
    Task<Series?> GetWithRankingsAsync(Guid seriesId);
}
