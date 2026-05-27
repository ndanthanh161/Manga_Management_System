using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class CandidateApplication
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public CandidateApplicationStatus Status { get; set; } = CandidateApplicationStatus.Pending;
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    // FK → User (pending account)
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // FK → Series (draft)
    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    // FK → EB member whose vote triggered approval
    public Guid? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<ApplicationVote> ApplicationVotes { get; set; } = new List<ApplicationVote>();
}
