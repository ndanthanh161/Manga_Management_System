namespace MMS.Domain.Entities;

public class Annotation
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid PageId { get; set; }
    public Page Page { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
