namespace MMS.Domain.Events.Annotations;

public record AnnotationCreatedEvent(
    Guid AnnotationId,
    Guid PageId,
    Guid ChapterId,
    Guid CreatedByUserId,
    string Category,
    double X,
    double Y,
    double Width,
    double Height
) : DomainEvent;
