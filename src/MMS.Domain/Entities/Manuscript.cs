using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Manuscript
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ManuscriptStatus Status { get; set; } = ManuscriptStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Bản thảo thuộc lần nộp nào
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
