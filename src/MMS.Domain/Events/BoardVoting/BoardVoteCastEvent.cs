namespace MMS.Domain.Events.BoardVoting;

public record BoardVoteCastEvent(
    Guid SubmissionId,
    int ApproveCount,
    int RejectCount,
    int TotalVotes
) : DomainEvent;
