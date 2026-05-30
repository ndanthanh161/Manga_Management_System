using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface ISeriesRepository : IGenericRepository<Series>
{
    Task<IEnumerable<Series>> GetByAuthorIdAsync(Guid authorId);
    Task<Series?> GetWithRankingsAsync(Guid seriesId);
    Task<IEnumerable<Series>> GetAllWithRankingsAndAuthorAsync();
    Task<IEnumerable<Series>> GetPendingSeriesAsync();
    Task AddRankingAsync(Ranking ranking);
}
