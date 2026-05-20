using MMS.Domain.Enums;

namespace MMS.Domain.Entities;

public class WorkTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Todo;
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public Guid? RegionId { get; set; }
    public Region? Region { get; set; }

    public string? ResourceUrl { get; set; }
    public string? ResultFileUrl { get; set; }
    public string? ResultNote { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal Reward { get; set; } = 0;
}
