using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class SubmissionTransition
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;
    public WorkflowStatus FromStatus { get; set; }
    public WorkflowStatus ToStatus { get; set; }
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
