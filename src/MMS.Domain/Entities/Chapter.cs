namespace MMS.Domain.Entities;

public class Chapter
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double ChapterNumber { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    public ICollection<Page> Pages { get; set; } = new List<Page>();
}
