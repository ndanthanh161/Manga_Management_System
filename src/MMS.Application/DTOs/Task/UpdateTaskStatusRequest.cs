using MMS.Domain.Enums;

namespace MMS.Application.DTOs.Task;

public record UpdateTaskStatusRequest(
    AppTaskStatus NewStatus
);
