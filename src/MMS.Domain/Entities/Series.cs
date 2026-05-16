using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Series
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AlternativeTitle { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public SeriesStatus Status { get; set; } = SeriesStatus.Ongoing; // Use Enum
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public ICollection<ReaderVote> ReaderVotes { get; set; } = new List<ReaderVote>();
    public ICollection<Ranking> Rankings { get; set; } = new List<Ranking>();
}
