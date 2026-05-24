using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IAnnotationRepository : IGenericRepository<Annotation>
{
    Task<IEnumerable<Annotation>> GetByPageIdAsync(Guid pageId);

    // Cho dashboard — đếm chưa Closed theo từng page của chapter
    Task<Dictionary<Guid, int>> GetUnresolvedCountByChapterAsync(Guid chapterId);
}
