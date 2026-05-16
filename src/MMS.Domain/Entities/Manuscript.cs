using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Manuscript
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ManuscriptStatus Status { get; set; } = ManuscriptStatus.Draft; // Use Enum
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
