namespace MMS.Domain.Entities;

public class Region
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Tọa độ bounding box trên trang truyện (tính bằng %)
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Guid? PageId { get; set; }
    public Page? Page { get; set; }

    public ICollection<Task> Tasks { get; set; } = new List<Task>();
}
