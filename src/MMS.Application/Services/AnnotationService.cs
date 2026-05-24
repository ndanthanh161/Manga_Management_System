using Mapster;
using MMS.Application.Common;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;

namespace MMS.Application.Services;

public class AnnotationService : IAnnotationService
{
    private readonly IUnitOfWork _uow;

    public AnnotationService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AnnotationResponse> CreateAsync(Guid userId, CreateAnnotationRequest request)
    {
        var annotation = request.Adapt<Annotation>();
        annotation.Id = Guid.NewGuid();
        annotation.UserId = userId;

        await _uow.Annotations.AddAsync(annotation);
        await _uow.SaveChangesAsync();

        // Reload kèm User để Adapt đúng UserName
        var annotations = await _uow.Annotations.GetByPageIdAsync(annotation.PageId);
        var created = annotations.First(a => a.Id == annotation.Id);
        return created.Adapt<AnnotationResponse>();
    }

    public Task<Result<bool>> DeleteAsync(Guid userId, Guid annotationId)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<AnnotationResponse>> GetByPageAsync(Guid pageId)
    {
        var annotations = await _uow.Annotations.GetByPageIdAsync(pageId);
        return annotations.Adapt<IEnumerable<AnnotationResponse>>();
    }

    public Task<Result<IEnumerable<UnresolvedCountResponse>>> GetUnresolvedCountByChapterAsync(Guid chapterId)
    {
        throw new NotImplementedException();
    }

    public Task<Result<AnnotationResponse>> ResolveAsync(Guid userId, Guid annotationId)
    {
        throw new NotImplementedException();
    }

    public Task<Result<AnnotationResponse>> ReviewAsync(Guid userId, Guid annotationId, ReviewAnnotationRequest request)
    {
        throw new NotImplementedException();
    }

    Task<Result<AnnotationResponse>> IAnnotationService.CreateAsync(Guid userId, CreateAnnotationRequest request)
    {
        throw new NotImplementedException();
    }

    Task<Result<IEnumerable<AnnotationResponse>>> IAnnotationService.GetByPageAsync(Guid pageId)
    {
        throw new NotImplementedException();
    }
}
