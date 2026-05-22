using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Manuscript
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ManuscriptStatus Status { get; set; } = ManuscriptStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;  
    public bool IsCurrentVersion { get; set; } = true;
    
    // Parent version reference
    public Guid? ParentManuscriptId { get; set; }
    public Manuscript? ParentManuscript { get; set; }
    
    public Guid ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    // Nullable Submission (assigned when submitted)
    public Guid? SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<ManuscriptPageRef> PageRefs { get; set; } = new List<ManuscriptPageRef>();
}
