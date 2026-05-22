using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    // Bộ truyện được nộp xét duyệt
    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    // Mangaka nộp
    public Guid? MangakaId { get; set; }
    public User? Mangaka { get; set; }

    // Biên tập viên phụ trách (Tantou)
    public Guid? AssignedTantouId { get; set; }
    public User? AssignedTantou { get; set; }

    public ICollection<BoardVote> Votes { get; set; } = new List<BoardVote>();
    public ICollection<Manuscript> Manuscripts { get; set; } = new List<Manuscript>();
    public ICollection<SubmissionTransition> Transitions { get; set; } = new List<SubmissionTransition>();
}
