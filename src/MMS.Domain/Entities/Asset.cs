namespace MMS.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Image, PSD, ZIP, PDF...
    public string Type { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string PublicId { get; set; } = string.Empty;

    public long Size { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // ===== Manuscript =====
    public Guid? ManuscriptId { get; set; }

    public Manuscript? Manuscript { get; set; }

    // ===== Candidate Application =====
    public Guid? CandidateApplicationId { get; set; }

    public CandidateApplication? CandidateApplication { get; set; }
}