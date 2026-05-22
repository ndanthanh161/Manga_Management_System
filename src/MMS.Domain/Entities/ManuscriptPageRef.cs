namespace MMS.Domain.Entities;

public class ManuscriptPageRef
{
    public Guid Id { get; set; }
    public Guid ManuscriptId { get; set; }
    public Manuscript Manuscript { get; set; } = null!;
    public Guid PageId { get; set; }
    public Page Page { get; set; } = null!;
    public int PageVersion { get; set; }
}
