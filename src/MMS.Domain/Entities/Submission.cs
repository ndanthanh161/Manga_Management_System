using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending; // Use Enum
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BoardVote> Votes { get; set; } = new List<BoardVote>();
}
