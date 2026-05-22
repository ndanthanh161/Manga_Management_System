using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IManuscriptPageRefRepository : IGenericRepository<ManuscriptPageRef>
{
    Task<List<ManuscriptPageRef>> GetByManuscriptAsync(Guid manuscriptId);
}
