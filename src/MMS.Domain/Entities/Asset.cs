namespace MMS.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Image, PSD, etc.
    public string FileUrl { get; set; } = string.Empty;
    public long Size { get; set; }

    public Guid ManuscriptId { get; set; }
    public Manuscript Manuscript { get; set; } = null!;
}
