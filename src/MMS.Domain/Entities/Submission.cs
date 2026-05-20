using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Bộ truyện được nộp xét duyệt
    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    // Mangaka nộp
    public Guid? MangakaId { get; set; }
    public User? Mangaka { get; set; }

    public ICollection<BoardVote> Votes { get; set; } = new List<BoardVote>();
    public ICollection<Manuscript> Manuscripts { get; set; } = new List<Manuscript>();
}
