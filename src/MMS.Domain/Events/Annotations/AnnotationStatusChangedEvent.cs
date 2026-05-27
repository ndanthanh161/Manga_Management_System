using MMS.Domain.Enums;

namespace MMS.Domain.Events.Annotations;

public record AnnotationStatusChangedEvent(
    Guid AnnotationId,
    Guid ChapterId,
    AnnotationStatus OldStatus,
    AnnotationStatus NewStatus,
    Guid ActorUserId
) : DomainEvent;
