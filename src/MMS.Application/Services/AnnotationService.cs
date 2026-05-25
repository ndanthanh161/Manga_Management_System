using Mapster;
using MMS.Application.Common;
using MMS.Application.DTOs.Annotation;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Application.Interfaces.Repositories;
using MMS.Application.Validators;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Services;

public class AnnotationService : IAnnotationService
{
    private readonly IUnitOfWork _uow;

    public AnnotationService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<AnnotationResponse>> CreateAsync(Guid userId, CreateAnnotationRequest request)
    {
        var validationResult = AnnotationValidator.ValidateCreate(request);
        if (!validationResult.Succeeded)
            return Result<AnnotationResponse>.Failure(validationResult.ErrorCode!, validationResult.Message!);

        var page = await _uow.Pages.GetByIdAsync(request.PageId);
        if (page == null)
            return Result<AnnotationResponse>.NotFound("PageNotFound", "The specified page does not exist.");

        if (!page.IsCurrentVersion)
            return Result<AnnotationResponse>.Failure("PageNotCurrentVersion", "Annotations can only be added to the current version of a page.");

        var chapter = await _uow.Chapters.GetByIdAsync(page.ChapterId);
        if (chapter != null)
        {
            
        }

        var annotation = new Annotation
        {
            PageId = request.PageId,
            UserId = userId,
            Content = request.Content,
            Category = request.Category,
            Shape = request.Shape,
            X = request.X,
            Y = request.Y,
            Width = request.Width,
            Height = request.Height,
            StrokeData = request.StrokeData,
            Status = AnnotationStatus.Open
        };

        await _uow.Annotations.AddAsync(annotation);
        await _uow.SaveChangesAsync();

        var response = await GetAnnotationResponseAsync(annotation.Id);
        return Result<AnnotationResponse>.Success(response!);
    }

    public async Task<Result<AnnotationResponse>> ResolveAsync(Guid userId, Guid annotationId)
    {
        var annotation = await _uow.Annotations.GetByIdAsync(annotationId);
        if (annotation == null)
            return Result<AnnotationResponse>.NotFound("AnnotationNotFound", "The annotation does not exist.");

        if (annotation.Status != AnnotationStatus.Open)
            return Result<AnnotationResponse>.Failure("NOT_OPEN", "Chỉ có thể Resolve khi đang Open");

        annotation.Status = AnnotationStatus.Resolved;
        annotation.ResolvedAt = DateTime.UtcNow;
        annotation.ResolvedByUserId = userId;
        annotation.UpdatedAt = DateTime.UtcNow;

        _uow.Annotations.Update(annotation);
        await _uow.SaveChangesAsync();

        var response = await GetAnnotationResponseAsync(annotation.Id);
        return Result<AnnotationResponse>.Success(response!);
    }

    public async Task<Result<AnnotationResponse>> ReviewAsync(Guid userId, Guid annotationId, ReviewAnnotationRequest request)
    {
        var annotation = await _uow.Annotations.GetByIdAsync(annotationId);
        if (annotation == null)
            return Result<AnnotationResponse>.NotFound("AnnotationNotFound", "The annotation does not exist.");

        if (annotation.Status != AnnotationStatus.Resolved)
            return Result<AnnotationResponse>.Failure("NOT_RESOLVED", "Chỉ có thể Review khi đang Resolved");

        if (request.IsApproved)
        {
            annotation.Status = AnnotationStatus.Closed;
            annotation.ClosedAt = DateTime.UtcNow;
            annotation.ClosedByUserId = userId;
        }
        else
        {
            annotation.Status = AnnotationStatus.Open;
            if (!string.IsNullOrWhiteSpace(request.Feedback))
            {
                annotation.Content += $"\n\n[Tantou Update {DateTime.UtcNow:yyyy-MM-dd HH:mm}]: {request.Feedback}";
            }
        }
        
        annotation.UpdatedAt = DateTime.UtcNow;

        _uow.Annotations.Update(annotation);
        await _uow.SaveChangesAsync();

        var response = await GetAnnotationResponseAsync(annotation.Id);
        return Result<AnnotationResponse>.Success(response!);
    }

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid annotationId)
    {
        var annotation = await _uow.Annotations.GetByIdAsync(annotationId);
        if (annotation == null)
            return Result<bool>.NotFound("AnnotationNotFound", "The annotation does not exist.");

        if (annotation.UserId != userId)
            return Result<bool>.Failure("FORBIDDEN", "You can only delete your own annotations.");

        if (annotation.Status != AnnotationStatus.Open)
            return Result<bool>.Failure("NOT_OPEN", "You can only delete Open annotations.");

        _uow.Annotations.Remove(annotation);
        await _uow.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<AnnotationResponse>>> GetByPageAsync(Guid pageId)
    {
        var annotations = await ((IAnnotationRepository)_uow.Annotations).GetByPageIdAsync(pageId);
        
        var responses = annotations.Adapt<List<AnnotationResponse>>();
        return Result<IEnumerable<AnnotationResponse>>.Success(responses);
    }

    public async Task<Result<IEnumerable<UnresolvedCountResponse>>> GetUnresolvedCountByChapterAsync(Guid chapterId)
    {
        var countDict = await ((IAnnotationRepository)_uow.Annotations).GetUnresolvedCountByChapterAsync(chapterId);
        
        var resultList = new List<UnresolvedCountResponse>();
        foreach (var kvp in countDict)
        {
            var page = await _uow.Pages.GetByIdAsync(kvp.Key);
            if (page != null)
            {
                resultList.Add(new UnresolvedCountResponse(kvp.Key, page.PageNumber, kvp.Value));
            }
        }

        return Result<IEnumerable<UnresolvedCountResponse>>.Success(resultList);
    }

    private async Task<AnnotationResponse?> GetAnnotationResponseAsync(Guid id)
    {
        var annotation = await _uow.Annotations.GetByIdAsync(id);
        if (annotation == null) return null;
        
        if (annotation.User == null && annotation.UserId != Guid.Empty)
            annotation.User = await _uow.Users.GetByIdAsync(annotation.UserId);
        
        if (annotation.ResolvedByUser == null && annotation.ResolvedByUserId.HasValue)
            annotation.ResolvedByUser = await _uow.Users.GetByIdAsync(annotation.ResolvedByUserId.Value);

        if (annotation.ClosedByUser == null && annotation.ClosedByUserId.HasValue)
            annotation.ClosedByUser = await _uow.Users.GetByIdAsync(annotation.ClosedByUserId.Value);

        return annotation.Adapt<AnnotationResponse>();
    }
}
