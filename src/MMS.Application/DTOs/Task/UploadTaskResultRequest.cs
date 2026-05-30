using Microsoft.AspNetCore.Http;

namespace MMS.Application.DTOs.Task;

public record UploadTaskResultRequest(
    Guid TaskId,
    IFormFile File
);
