namespace MMS.Domain.Enums;

public enum WorkflowStatus
{
    Draft,
    Submitted,
    UnderTantouReview,
    ReturnedForRevision,
    ForwardedToBoard,
    InBoardVoting,
    Approved,
    Rejected,
    Postponed
}
