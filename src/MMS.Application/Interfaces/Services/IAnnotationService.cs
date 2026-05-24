using MMS.Application.Common;

namespace MMS.Application.Interfaces.Services;

public interface IAnnotationService
{
    Task<Result<AnnotationResponse>> CreateAsync(Guid userId, CreateAnnotationRequest request);
    Task<Result<AnnotationResponse>> ResolveAsync(Guid userId, Guid annotationId);
    Task<Result<AnnotationResponse>> ReviewAsync(Guid userId, Guid annotationId, ReviewAnnotationRequest request);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid annotationId);
    Task<Result<IEnumerable<AnnotationResponse>>> GetByPageAsync(Guid pageId);
    Task<Result<IEnumerable<UnresolvedCountResponse>>> GetUnresolvedCountByChapterAsync(Guid chapterId);
}
