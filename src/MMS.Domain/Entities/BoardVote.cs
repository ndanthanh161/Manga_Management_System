using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class BoardVote
{
    public Guid Id { get; set; }
    public VoteType VoteType { get; set; } = VoteType.Neutral; // Use Enum
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public Guid BoardMemberId { get; set; }
    public User BoardMember { get; set; } = null!;
}
