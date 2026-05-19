using MMS.Application.DTOs.Region;

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
    RegionResponse? Region,
    string? PageImageUrl,
    string? ResourceUrl,
    string? ResultFileUrl,
    string? ResultNote,
    DateTime? CompletedAt,
    DateTime CreatedAt
);
