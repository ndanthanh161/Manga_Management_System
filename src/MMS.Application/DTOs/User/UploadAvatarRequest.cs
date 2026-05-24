using Microsoft.AspNetCore.Http;

namespace MMS.Application.DTOs.User;

public class UploadAvatarRequest
{
    public IFormFile File { get; set; } = null!;
}
