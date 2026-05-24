using MMS.Domain.Enums;

namespace MMS.Application.DTOs.Annotation;

public record CreateAnnotationRequest(
    Guid PageId,
    string Content,
    AnnotationCategory Category,
    AnnotationShape Shape,
    double X,
    double Y,
    double Width,
    double Height,
    string? StrokeData
);
