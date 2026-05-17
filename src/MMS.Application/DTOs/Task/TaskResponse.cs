namespace MMS.Application.DTOs.Task;

public record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    DateTime? DueDate,
    Guid? AssignedUserId,
    string? AssignedUserName,
    Guid? RegionId,
    DateTime CreatedAt
);
