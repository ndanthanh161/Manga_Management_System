namespace MMS.Domain.Entities;

public class Page
{
    public Guid Id { get; set; }
    public int PageNumber { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? CloudinaryPublicId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }

    public Guid ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public ICollection<Annotation> Annotations { get; set; } = new List<Annotation>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
