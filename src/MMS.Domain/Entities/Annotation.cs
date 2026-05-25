using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Annotation
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public AnnotationCategory Category { get; set; }
    public AnnotationShape Shape { get; set; }
    public AnnotationStatus Status { get; set; }
    public double Height { get; set; }
    public double Width { get; set; }
    public string? StrokeData { get; set; } // Dữ liệu vẽ tay, lưu dưới dạng JSON hoặc SVG path
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }

    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public User? ClosedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid PageId { get; set; }
    public Page Page { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
