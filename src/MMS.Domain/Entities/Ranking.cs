namespace MMS.Domain.Entities;

public class Ranking
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public string Period { get; set; } = "Weekly";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;
}
