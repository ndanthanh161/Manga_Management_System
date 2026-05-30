using MMS.Domain.Enums;

namespace MMS.Application.DTOs.Task;

public record UpdateTaskStatusRequest(
    WorkTaskStatus NewStatus,
    string? ResultFileUrl = null,
    string? ResultNote = null
);
