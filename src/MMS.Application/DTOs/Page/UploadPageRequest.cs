using Microsoft.AspNetCore.Http;
using System;

namespace MMS.Application.DTOs.Page;

public class UploadPageRequest
{
    public IFormFile File { get; set; } = null!;
    public Guid ChapterId { get; set; }
    public int PageNumber { get; set; }
}
