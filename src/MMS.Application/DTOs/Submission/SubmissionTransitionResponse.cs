namespace MMS.Application.DTOs.Submission;

public record SubmissionTransitionResponse(
    Guid Id,
    Guid SubmissionId,
    string FromStatus,
    string ToStatus,
    Guid ActorUserId,
    string ActorName,
    string Comment,
    DateTime OccurredAt
);
