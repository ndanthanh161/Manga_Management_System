using Microsoft.AspNetCore.Http;
using MMS.Domain.Enums;

namespace MMS.Application.DTOs.Task;

public record UpdateTaskStatusRequest(
    WorkTaskStatus NewStatus,
    string? ResultFileUrl = null,
    string? ResultNote = null
);

public class UploadTaskResultRequest
{
    public Guid TaskId { get; set; }
    public IFormFile File { get; set; } = null!;
}
