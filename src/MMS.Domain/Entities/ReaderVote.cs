namespace MMS.Domain.Entities;

public class ReaderVote
{
    public Guid Id { get; set; }
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
