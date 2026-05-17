namespace MMS.Application.DTOs.Annotation;

public record AnnotationResponse(
    Guid Id,
    Guid PageId,
    Guid UserId,
    string UserName,
    string Content,
    double X,
    double Y,
    DateTime CreatedAt
);
