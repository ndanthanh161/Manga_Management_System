using Microsoft.AspNetCore.Http;
using System;

namespace MMS.Application.DTOs.Series;

public class UploadCoverRequest
{
    public IFormFile File { get; set; } = null!;
    public Guid SeriesId { get; set; }
}
