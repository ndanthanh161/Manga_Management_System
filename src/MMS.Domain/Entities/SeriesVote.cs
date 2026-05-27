using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class SeriesVote
{
    public Guid Id { get; set; }
    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;
    public Guid BoardMemberId { get; set; }
    public User BoardMember { get; set; } = null!;
    public VoteType VoteType { get; set; }
    public Guid? ProposedTantouId { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
