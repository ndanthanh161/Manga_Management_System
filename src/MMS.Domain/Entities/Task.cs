using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class Task
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AppTaskStatus Status { get; set; } = AppTaskStatus.Todo;
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public Guid? RegionId { get; set; }
    public Region? Region { get; set; }
}
