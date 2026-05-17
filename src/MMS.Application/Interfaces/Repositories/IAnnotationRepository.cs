using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IAnnotationRepository : IGenericRepository<Annotation>
{
    Task<IEnumerable<Annotation>> GetByPageIdAsync(Guid pageId);
}
