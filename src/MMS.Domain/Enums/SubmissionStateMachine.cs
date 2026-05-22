using MMS.Domain.Exceptions;

namespace MMS.Domain.Enums;

public static class SubmissionStateMachine
{
    private static readonly Dictionary<WorkflowStatus, HashSet<WorkflowStatus>> AllowedTransitions = new()
    {
        {
            WorkflowStatus.Draft, new HashSet<WorkflowStatus> { WorkflowStatus.Submitted }
        },
        {
            WorkflowStatus.Submitted, new HashSet<WorkflowStatus> { WorkflowStatus.UnderTantouReview }
        },
        {
            WorkflowStatus.UnderTantouReview, new HashSet<WorkflowStatus> { WorkflowStatus.ReturnedForRevision, WorkflowStatus.ForwardedToBoard }
        },
        {
            WorkflowStatus.ReturnedForRevision, new HashSet<WorkflowStatus> { WorkflowStatus.Submitted, WorkflowStatus.Draft }
        },
        {
            WorkflowStatus.ForwardedToBoard, new HashSet<WorkflowStatus> { WorkflowStatus.InBoardVoting, WorkflowStatus.Approved, WorkflowStatus.Rejected, WorkflowStatus.Postponed }
        },
        {
            WorkflowStatus.InBoardVoting, new HashSet<WorkflowStatus> { WorkflowStatus.Approved, WorkflowStatus.Rejected, WorkflowStatus.Postponed }
        },
        {
            WorkflowStatus.Approved, new HashSet<WorkflowStatus>() // Terminal
        },
        {
            WorkflowStatus.Rejected, new HashSet<WorkflowStatus>() // Terminal
        },
        {
            WorkflowStatus.Postponed, new HashSet<WorkflowStatus>() // Terminal
        }
    };

    public static bool CanTransition(WorkflowStatus from, WorkflowStatus to)
    {
        if (from == to) return true;
        return AllowedTransitions.TryGetValue(from, out var nextStates) && nextStates.Contains(to);
    }

    public static IEnumerable<WorkflowStatus> GetAllowedNextStates(WorkflowStatus current)
    {
        return AllowedTransitions.TryGetValue(current, out var nextStates) ? nextStates : Enumerable.Empty<WorkflowStatus>();
    }

    public static void ValidateTransition(WorkflowStatus from, WorkflowStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidWorkflowTransitionException($"Không thể chuyển trạng thái từ {from} sang {to}.");
        }
    }
}
