using MMS.Domain.Enums;

namespace MMS.Application.DTOs.Annotation;

public record AnnotationResponse(
    Guid Id,
    Guid PageId,
    Guid UserId,
    string UserName,
    string Content,
    AnnotationCategory Category,
    AnnotationShape Shape,
    double X,
    double Y,
    double Width,
    double Height,
    string? StrokeData,
    string Status, // Enum string (Open/Resolved/Closed)
    DateTime? ResolvedAt,
    Guid? ResolvedByUserId,
    string? ResolvedByUserName,
    DateTime? ClosedAt,
    Guid? ClosedByUserId,
    string? ClosedByUserName,
    DateTime CreatedAt
);
