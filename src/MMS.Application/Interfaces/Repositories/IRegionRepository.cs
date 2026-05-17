using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IRegionRepository : IGenericRepository<Region>
{
    Task<IEnumerable<Region>> GetByPageIdAsync(Guid pageId);
}
