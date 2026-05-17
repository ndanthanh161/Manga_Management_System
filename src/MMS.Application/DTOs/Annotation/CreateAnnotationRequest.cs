namespace MMS.Application.DTOs.Annotation;

public record CreateAnnotationRequest(
    Guid PageId,
    string Content,
    double X,
    double Y
);
