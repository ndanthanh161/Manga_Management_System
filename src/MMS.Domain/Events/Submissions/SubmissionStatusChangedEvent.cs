using MMS.Domain.Enums;

namespace MMS.Domain.Events.Submissions;

public record SubmissionStatusChangedEvent(
    Guid SubmissionId,
    Guid SeriesId,
    WorkflowStatus OldStatus,
    WorkflowStatus NewStatus,
    Guid ActorUserId,
    Guid NotifyUserId,
    string Message
) : DomainEvent;
