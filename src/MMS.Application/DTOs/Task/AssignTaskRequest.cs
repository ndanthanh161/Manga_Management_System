namespace MMS.Application.DTOs.Task;

public record AssignTaskRequest(
    string Title,
    string Description,
    Guid AssistantId,
    Guid RegionId,
    string Priority,
    DateTime? DueDate,
    string? ResourceUrl = null,
    decimal Reward = 0
);
