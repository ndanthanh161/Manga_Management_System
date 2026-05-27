namespace MMS.Domain.Events.BoardVoting;

public record ApplicationVoteCastEvent(
    Guid ApplicationId,
    int ApproveCount,
    int RejectCount,
    int TotalVotes
) : DomainEvent;
