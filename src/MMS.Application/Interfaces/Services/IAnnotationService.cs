namespace MMS.Application.Interfaces.Services;

public interface IAnnotationService
{
    Task<AnnotationResponse> CreateAsync(Guid userId, CreateAnnotationRequest request);
    Task<IEnumerable<AnnotationResponse>> GetByPageAsync(Guid pageId);
}
